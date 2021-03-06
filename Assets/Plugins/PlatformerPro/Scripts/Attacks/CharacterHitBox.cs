using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PlatformerPro
{
	/// <summary>
	/// The damage causing collider of a character or enemy, collides with hurt boxes to cause damage.
	/// </summary>
	public class CharacterHitBox : PlatformerProMonoBehaviour, ICharacterReference
	{

		/// <summary>
		/// If non-null we will apply the damage type from the weapon equipped in the given weapon slot, overriding any
		/// damage type set on the attack.
		/// </summary>
		public string weaponSlot;
		
		/// <summary>
		/// The character this hit box is for.
		/// </summary>
		protected IMob character;

		/// <summary>
		/// The actual collider.
		/// </summary>
		protected Collider2D myCollider;

		/// <summary>
		/// Tracks the time for enalbing and disabling the hit box.
		/// </summary>
		protected float hitTimer;

		/// <summary>
		/// Tracks if this attack instance has hit an enemy.
		/// </summary>
		protected bool hasHitCharacter;

		/// <summary>
		/// Cached damage info.
		/// </summary>
		protected DamageInfo damageInfo;

		/// <summary>
		/// Gets the header string used to describe the component.
		/// </summary>
		/// <value>The header.</value>
		override public string Header
		{
			get
			{
				return "The damage causing collider of a character or enemy, collides with hurt boxes to cause damage.";
			}
		}

		/// <summary>
		/// Gets the character.
		/// </summary>
		virtual public Character Character {
			get
			{
				return character as Character;
			}
			set
			{
				character = value;
			}
		}

		/// <summary>
		/// Returns true if the hit box has hit something since it was last enabled.
		/// </summary>
		virtual public bool HasHit
		{
			get
			{
				return hasHitCharacter;
			}
		}

		/// <summary>
		/// Init this instance, this should be called by the attack system during Start();
		/// </summary>
		virtual public void Init(DamageInfo info)
		{
			character = (IMob) gameObject.GetComponentInParent (typeof(IMob));
			myCollider = GetComponent<Collider2D>();
			if (myCollider == null)
			{
				Debug.LogError("A CharacterHitBox must be on the same GameObject as a Collider2D");
			}

			DamageType damageType = info.DamageType;
			if (!string.IsNullOrEmpty(weaponSlot))
			{
				ItemInstanceData weapon = Character.EquipmentManager.GetItemForSlot(weaponSlot);
				if (weapon != null && weapon.Data != null) damageType = weapon.Data.damageType;
			}
			damageInfo = new DamageInfo (info.Amount, damageType, Vector2.zero, character);
		}

		/// <summary>
		/// Updates the damage info with new values.
		/// </summary>
		/// <param name="amount">Amount.</param>
		/// <param name="damageType">Damage type.</param>
		virtual public void UpdateDamageInfo(int amount, DamageType damageType)
		{
			DamageType actualDamageType = damageType;
			if (!string.IsNullOrEmpty(weaponSlot))
			{
				ItemInstanceData weapon = Character.EquipmentManager.GetItemForSlot(weaponSlot);
				if (weapon != null && weapon.Data != null) actualDamageType = weapon.Data.damageType;
			}
			damageInfo.Amount = amount;
			damageInfo.DamageType = actualDamageType;
		}

		/// <summary>
		/// Start the hit with no timer.
		/// </summary>
		virtual public void EnableImmediate() 
		{
			hasHitCharacter = false;
			myCollider.enabled = true;
		}

		/// <summary>
		/// Stop the hit immedaitely.
		/// </summary>
		virtual public void DisableImmediate() 
		{
			hasHitCharacter = false;
			myCollider.enabled = false;
		}
		
		/// <summary>
		/// Start the hit.
		/// </summary>
		virtual public void EnableImmediate(float enableTime, float disableTime)
		{
			// Disable then restart
			// TODO It may be faster to do this with physics by ignoring layers
			myCollider.enabled = false;
			StopAllCoroutines();
			StartCoroutine(DoEnable (enableTime, disableTime));
		}

		/// <summary>
		/// Forces the attack to finish.
		/// </summary>
		virtual public void ForceStop()
		{
			StopAllCoroutines();
			myCollider.enabled = false;
			hitTimer = 0.0f;
		}

		/// <summary>
		/// Turn on the hit box.
		/// </summary>.
		/// <returns>The enable.</returns>
		/// <param name="enableTime">Enable time.</param>
		/// <param name="disableTime">Disable time.</param>
		virtual protected IEnumerator DoEnable(float enableTime, float disableTime)
		{
			hasHitCharacter = false;
			hitTimer = 0.0f;
			// Handle the timing, we don't use WaitForSeconds as we want to align with the internal frame time
			while (hitTimer < enableTime)
			{
				hitTimer += TimeManager.FrameTime;
				yield return true;
			}
			myCollider.enabled = true;
			while (hitTimer < disableTime)
			{
				hitTimer += TimeManager.FrameTime;
				yield return true;
			}
			myCollider.enabled = false;
		}

		/// <summary>
		/// Unity 2D trigger hook.
		/// </summary>
		/// <param name="other">Other.</param>
		void OnTriggerEnter2D(Collider2D other)
		{
			DoHit(other);
		}

		/// <summary>
		/// Do the actual hit.
		/// </summary>
		/// <param name="other">Other.</param>
		/// <returns>true if a hit was done.</returns>
		virtual protected bool DoHit(Collider2D other)
		{
			IHurtable hurtBox = (IHurtable) other.gameObject.GetComponent(typeof(IHurtable));
			// Got a hurt box and its not ourselves
			if (character != null && hurtBox != null && !hasHitCharacter && hurtBox.Mob != character )
			{
				damageInfo.Direction = transform.position - other.transform.position;
				damageInfo.DamageCauser = character;
				hurtBox.Damage(damageInfo);
				if (character is Character) ((Character)character).HitEnemy(hurtBox.Mob, damageInfo);
				hasHitCharacter = true;
				return true;
			}
			return false;
		}
	}
}