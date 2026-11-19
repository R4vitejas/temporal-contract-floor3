using System.Collections.Generic;
using System.Linq;
using TemporalContract.Combat;
using TemporalContract.Players;
using UnityEngine;

namespace TemporalContract.Feedback
{
    public sealed class ProductionCombatPresenter : MonoBehaviour
    {
        public FeedbackPresenter presenter;
        public FeedbackCueLibrary library;
        public CombatFeedbackHooks hooks;
        private readonly List<NetworkHitscanWeapon> weapons=new List<NetworkHitscanWeapon>();
        private NetworkPlayerHealth localHealth;
        private float nextScan;
        private void Update()
        {
            if(Time.unscaledTime<nextScan)return;nextScan=Time.unscaledTime+.2f;
            weapons.RemoveAll(w=>!w);
            foreach(var weapon in FindObjectsByType<NetworkHitscanWeapon>(FindObjectsSortMode.None))
                if(!weapons.Contains(weapon)){weapons.Add(weapon);weapon.ShotFired+=OnShot;weapon.HitConfirmed+=OnHit;}
            var player=NetworkFpsPlayer.LocalOwner;
            var health=player?player.GetComponent<NetworkPlayerHealth>():null;
            if(localHealth!=health)
            {
                if(localHealth)localHealth.DamageReceived-=OnDamage;
                localHealth=health;if(localHealth)localHealth.DamageReceived+=OnDamage;
            }
            var view=player?player.GetComponentInChildren<LocalWeaponView>(true):null;
            hooks.BindView(view?view.RecoilPivot:null,view?view.MuzzleFlash:null);
        }
        private void OnDisable()
        {
            foreach(var weapon in weapons.Where(w=>w)){weapon.ShotFired-=OnShot;weapon.HitConfirmed-=OnHit;}
            weapons.Clear();if(localHealth)localHealth.DamageReceived-=OnDamage;localHealth=null;
            hooks.BindView(null,null);
        }
        private void OnShot(NetworkHitscanWeapon weapon)
        {
            var position=weapon.transform.position+Vector3.up*1.62f;
            if(weapon.IsOwner)hooks.NotifyShot(position);
            else presenter.PlayCombat(library.weaponFire,position,true);
        }
        private void OnHit(NetworkHitscanWeapon weapon) {if(weapon.IsOwner)hooks.NotifyHitConfirmed();}
        private void OnDamage(NetworkPlayerHealth player,int amount,ulong attacker) => hooks.NotifyDamageReceived(amount);
    }
}
