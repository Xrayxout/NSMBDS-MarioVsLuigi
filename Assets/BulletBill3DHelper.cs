using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;

public class BulletBill3DHelper : MonoBehaviour
{
    public SpriteRenderer bulletReference;

    private void OnEnable()
    {
        if (bulletReference.flipX)
        {
            this.transform.localScale = new Vector3(this.transform.localScale.x * -1, this.transform.localScale.y, this.transform.localScale.z);
        }
        else
        {
            this.transform.localScale = new Vector3(this.transform.localScale.x * 1, this.transform.localScale.y, this.transform.localScale.z);
        }
    }
}
