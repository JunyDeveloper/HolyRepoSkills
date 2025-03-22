using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Photon.Pun;
using UnityEngine;

namespace JP_RepoHolySkills
{
    public class TimedDestroyer : MonoBehaviourPun
    {
        public float lifeTime = 10f;

        void Start()
        {
            StartCoroutine(SelfDestruct());
        }

        IEnumerator SelfDestruct()
        {
            yield return new WaitForSeconds(lifeTime);

            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }
}
