using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScifiOfficeLite {
    public class DemoDoor : MonoBehaviour {
        Animator anim;

        private void Start() {
            anim = GetComponent<Animator>();
        }

        private void OnTriggerEnter(Collider other) {
            if(other.gameObject.name == "Player") {
                anim.SetTrigger("Open");
            }
        }
    }
}