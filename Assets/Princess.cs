using DG.Tweening;
using PlatformerPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Princess : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var character = collision.gameObject.GetComponentInParent<Character>();
        if (character != null)
        {
            character.InputEnabled = false;
            character.CharacterHealth.SetInvulnerable(1000);
            character.CharacterHealth.CurrentHealth = 99999;

            var sequence = DOTween.Sequence();
            sequence.AppendCallback(() => transform.localRotation = Quaternion.identity);
            sequence.Append(transform.DOJump(character.transform.position + new Vector3(0,0.5f,0.1f), 2f, 1, 0.5f));
            sequence.AppendCallback(() => transform.SetParent(character.transform));
            sequence.AppendInterval(0.5f);
            sequence.Append(character.transform.DOMoveY(3f, 1f).SetRelative(true).SetEase(Ease.OutQuad).OnStart(() => character.transform.DOMoveZ(-5f, 4f).SetRelative(true).SetEase(Ease.Linear)));
            sequence.Append(character.transform.DOMoveY(-3f, 1f).SetRelative(true).SetEase(Ease.InQuad));
            sequence.AppendInterval(1f);
            //Fade out
            sequence.AppendCallback(() => UpventureGameManager.instance.PlayEnding());
        }
    }


}