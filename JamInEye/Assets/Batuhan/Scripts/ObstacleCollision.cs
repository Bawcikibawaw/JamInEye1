using Unity.VisualScripting;
using UnityEngine;

public class ObstacleCollision : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Dark
        if (collision.gameObject.tag == "Player")
        {
            MainAudioManager.Instance.Play("BrightHit");

        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //Bright
        if (collision.gameObject.tag == "Player")
        {
            MainAudioManager.Instance.Play("DarkHit");


        }
    }
}
