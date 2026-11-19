# Asset credits

## Feedback sounds (`Game/Assets/Project/Feedback/Audio/*.wav`)

All sounds are simple placeholders generated procedurally by
`Game/Assets/Project/Feedback/Editor/FeedbackAssetBuilder.cs` (sine tones and noise bursts, 22.05 kHz, mono).
They are original to this project, use no third-party audio and need no licence or attribution. Replace any clip
by editing `FeedbackCueLibrary.asset`; list added third-party assets in this file with source and licence.

| File | Used for |
|---|---|
| `door_move.wav`, `switch_click.wav`, `keycard_pickup.wav`, `cover_scrape.wav` | Positional interaction sounds |
| `accepted.wav`, `denied.wav` | Local confirmation and denial |
| `gunshot.wav`, `hit_confirm.wav`, `damage_taken.wav` | Combat presentation |
| `round_tone.wav` | Round and match transitions |
