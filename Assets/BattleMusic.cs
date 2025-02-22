using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleMusic : MonoBehaviour
{
    void Awake()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        int index = (LevelManager.currentLevel - 1) / 2;

        string path = string.Format("BattleMusic/lvl{0}_battleMusic", index);
        audioSource.clip = Resources.Load<AudioClip>(path);
        audioSource.Play();
    }
}
