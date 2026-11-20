using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TemporalContract.UI.Board
{
    /// <summary>
    /// Local-only floor map. Opening acquires gameplay-input capture and frees the cursor; closing releases the same
    /// capture and restores the previous cursor state unless another modal still holds the lock. Selection never
    /// leaves this client. Escape, the Close button, <see cref="ForceClose"/>, losing the session, disabling and
    /// destroying all close it cleanly.
    /// </summary>
    public sealed class FloorMapPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Image artworkImage;
        [SerializeField] private RectTransform regionsRoot;
        [SerializeField] private Text roomLabel;
        [SerializeField] private Button closeButton;
        [SerializeField] private FloorMapLayout layout;
        [Tooltip("An IModalInputCapture (LocalInputCapture or PlayerInputCaptureAdapter). Optional.")]
        [SerializeField] private MonoBehaviour captureComponent;
        [SerializeField] private Color regionIdle = new Color(1f, 1f, 1f, 0.02f);
        [SerializeField] private Color regionSelected = new Color(1f, 0.85f, 0.2f, 0.45f);

        private readonly List<Image> regionImages = new List<Image>();
        private IModalInputCapture capture;
        private CursorLockMode savedLock;
        private bool savedVisible;
        private bool wasListening;

        public bool IsOpen { get; private set; }
        public int LastClosedFrame {get;private set;}=-1;
        public int SelectedIndex { get; private set; } = -1;
        public int RegionCount => regionImages.Count;
        public FloorMapLayout Layout => layout;

        public event Action Opened;
        public event Action Closed;
        public event Action<int> RoomSelected;
        /// <summary>Raised with true when this panel captures input and false when it releases it.</summary>
        public event Action<bool> CaptureStateChanged;

        private void Awake()
        {
            capture = captureComponent as IModalInputCapture;
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            Rebuild();
            SetVisible(false);
        }

        private void OnDisable() => ReleaseIfOpen(restoreCursor: true);
        private void OnDestroy() => ReleaseIfOpen(restoreCursor: false);

        public void SetCapture(IModalInputCapture newCapture) => capture = newCapture;

        /// <summary>Replace the floor artwork and rooms at runtime; regions are rebuilt, selection cleared.</summary>
        public void SetLayout(FloorMapLayout newLayout)
        {
            layout = newLayout;
            Rebuild();
        }

        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            IsOpen = true;
            savedLock = Cursor.lockState;
            savedVisible = Cursor.visible;
            wasListening = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            capture?.SetCaptured(this, true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetVisible(true);
            Select(-1);
            CaptureStateChanged?.Invoke(true);
            Opened?.Invoke();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            ReleaseIfOpen(restoreCursor: true);
        }

        /// <summary>Called by session code on disconnect or scene change. Same as Close, never throws.</summary>
        public void ForceClose() => Close();

        public void Select(int index)
        {
            if (layout == null || index >= layout.rooms.Length)
            {
                index = -1;
            }

            SelectedIndex = index;
            for (int i = 0; i < regionImages.Count; i++)
            {
                regionImages[i].color = i == index ? regionSelected : regionIdle;
            }

            if (roomLabel != null)
            {
                roomLabel.text = index >= 0 ? layout.rooms[index].displayName : "Select a room";
            }

            if (index >= 0)
            {
                RoomSelected?.Invoke(index);
            }
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            NetworkManager manager = NetworkManager.Singleton;
            bool listening = manager != null && manager.IsListening;
            if (wasListening && !listening)
            {
                Close(); // disconnected or host stopped while the board was open
            }
        }

        private void ReleaseIfOpen(bool restoreCursor)
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            LastClosedFrame=Time.frameCount;
            capture?.SetCaptured(this, false);
            if (restoreCursor)
            {
                // Respect any other modal (for example the session menu) that still holds capture.
                bool stillCaptured = capture != null && capture.IsGameplayInputCaptured;
                if (!stillCaptured)
                {
                    Cursor.lockState = savedLock;
                    Cursor.visible = savedVisible;
                }

                SetVisible(false);
            }

            CaptureStateChanged?.Invoke(false);
            Closed?.Invoke();
        }

        private void SetVisible(bool visible)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(visible);
            }
        }

        private void Rebuild()
        {
            foreach (Image image in regionImages)
            {
                if (image != null)
                {
                    Destroy(image.gameObject);
                }
            }

            regionImages.Clear();
            SelectedIndex = -1;
            if (layout == null)
            {
                return;
            }

            if (artworkImage != null)
            {
                artworkImage.sprite = layout.ResolveArtwork();
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (int i = 0; i < layout.rooms.Length; i++)
            {
                int index = i;
                FloorMapLayout.Room room = layout.rooms[i];
                var go = new GameObject($"Region_{room.id}", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(regionsRoot, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = new Vector2(room.area.xMin, room.area.yMin);
                rect.anchorMax = new Vector2(room.area.xMax, room.area.yMax);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var image = go.GetComponent<Image>();
                image.color = regionIdle;
                go.GetComponent<Button>().onClick.AddListener(() => Select(index));
                regionImages.Add(image);

                if (layout.showRegionLabels)
                {
                    var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                    labelGo.transform.SetParent(go.transform, false);
                    var labelRect = (RectTransform)labelGo.transform;
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
                    var text = labelGo.GetComponent<Text>();
                    text.font = font;
                    text.fontSize = 16;
                    text.alignment = TextAnchor.MiddleCenter;
                    text.color = Color.black;
                    text.raycastTarget = false;
                    text.text = room.displayName;
                }
            }
        }
    }
}
