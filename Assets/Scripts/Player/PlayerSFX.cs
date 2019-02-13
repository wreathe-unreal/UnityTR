using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerSFX : MonoBehaviour
{
    private AudioSource playerSource;

    [Header("Volumes")]
    public float footMinVol = 0.22f;
    public float footMaxVol = 0.3f;

    [Header("Sound Files")]
    public AudioClip[] feetSounds;
    public AudioClip[] jumpSounds;
    public AudioClip[] grabSounds;
    public AudioClip[] vaultSounds;
    public AudioClip[] screamSounds;
    public AudioClip[] slapSounds;
    public AudioClip[] hitGroundSounds;
    public AudioClip[] deathSounds;
    public AudioClip[] whooshSounds;
    public AudioClip[] ladderSounds;

    private void Start()
    {
        playerSource = GetComponent<AudioSource>();
    }

    public void PlayFootSound()
    {
        PlayRandomSound(feetSounds, Random.Range(footMinVol, footMaxVol));
    }

    public void PlayLadderSounds()
    {
        PlayRandomSound(ladderSounds, 0.1f);
    }

    public void PlayJumpSound()
    {
        PlayRandomSound(jumpSounds, 0.6f);
    }

    public void PlayWhooshSound()
    {
        PlayRandomSound(whooshSounds, 0.1f);
    }

    public void PlayGrabSound()
    {
        PlayRandomSound(grabSounds, 0.6f);
    }

    public void PlayHitGroundSound()
    {
        PlayRandomSound(hitGroundSounds, 0.5f);
    }

    public void PlayVaultSound()
    {
        PlayRandomSound(vaultSounds, 0.6f);
    }

    public void PlayScreamSound()
    {
        PlayRandomSound(screamSounds, 0.8f);
    }

    public void PlaySlapSounds()
    {
        PlayRandomSound(slapSounds, 0.1f);
    }

    public void PlayDeathSounds()
    {
        PlayRandomSound(deathSounds, 0.6f);
    }

    private void PlayRandomSound(AudioClip[] sounds, float volume)
    {
        int random = Random.Range(0, sounds.Length - 1);
        playerSource.PlayOneShot(sounds[random], volume);
    }
}
