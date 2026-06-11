using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public AudioSource musicSource;
    public AudioSource sfxSource;

    public AudioClip hit, music, shoot;

    private void OnEnable()
    {
        EventBus.Instance.OnSpellCast += OnSpellCast;
        EventBus.Instance.OnDamage += OnDamage;
    }

    private void OnDisable()
    {
        EventBus.Instance.OnSpellCast -= OnSpellCast;
        EventBus.Instance.OnDamage -= OnDamage;
    }

    public void Start()
    {
        if (musicSource == null || music == null)
        {
            return;
        }

        musicSource.clip = music;
        musicSource.Play();
    }

    public void PlayHit()
    {
        if (sfxSource == null || hit == null)
        {
            return;
        }

        sfxSource.PlayOneShot(hit);
    }

    public void PlayShoot()
    {
        if (sfxSource == null || shoot == null)
        {
            return;
        }

        sfxSource.PlayOneShot(shoot);
    }

    private void OnSpellCast(SpellCaster caster, Spell spell)
    {
        if (caster != null && caster.team == Hittable.Team.PLAYER)
        {
            PlayShoot();
        }
    }

    private void OnDamage(Vector3 where, Damage damage, Hittable target)
    {
        if (target != null && target.team == Hittable.Team.PLAYER)
        {
            PlayHit();
        }
    }
}
