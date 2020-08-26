﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ss_tutorial
{
    public enum TransitionParameter
    {
        Move,
        Jump,
        ForceTransition,
        Grounded,
        Attack,
        ClickAnimation,
        TransitionIndex,
        Turbo,
        Turn,
    }

    public enum SS_Scenes
    {
        CharacterSelect,
        LedgeGrab,
        Main,
    }


    public class CharacterControl : MonoBehaviour
    {
        public PlayableCharacterType playableCharacterType;
        public Animator SkinnedMeshAnimator;
        public bool Turbo;
        public bool MoveUp;
        public bool MoveDown;
        public bool MoveRight;
        public bool MoveLeft;
        public bool Jump;
        public bool Attack;
        public LedgeChecker ledgeChecker;
        public AnimationProgress animationProgress;

        public GameObject ColliderEdgePrefab;
        public List<GameObject> BottomSpheres = new List<GameObject>();
        public List<GameObject> FrontSpheres = new List<GameObject>();
        public List<Collider> RagdollParts = new List<Collider>();

        public float GravityMultiplier;
        public float PullMultiplier;

        private List<TriggerDetector> TriggerDetectors = new List<TriggerDetector>();
        private Dictionary<string, GameObject> ChildObjects = new Dictionary<string, GameObject>();

        private Rigidbody rigid;
   
        public Rigidbody RIGID_BODY
        {
            get
            {
                if(rigid == null)
                {
                    rigid = GetComponent<Rigidbody>();
                }
                return rigid;
            }
        }

        private void Awake()
        {
            bool SwitchBack = false;

            if(!IsFacingForward())
            {
                SwitchBack = true;
            }
             
            FaceForward(true);
            SetColliderSpheres();

            if(SwitchBack)
            {
                FaceForward(false);
            }

            ledgeChecker = GetComponentInChildren<LedgeChecker>();
            animationProgress = GetComponent<AnimationProgress>();

            RegisterCharacter();
        }

        private void RegisterCharacter()
        {
            if(!CharacterManager.Instance.Characters.Contains(this))
            {
                CharacterManager.Instance.Characters.Add(this);
            }
        }

        public List<TriggerDetector> GetAllTriggers()
        {
            if(TriggerDetectors.Count == 0)
            {
                TriggerDetector[] arr = this.gameObject.GetComponentsInChildren<TriggerDetector>();

                foreach(TriggerDetector d in arr)
                {
                    TriggerDetectors.Add(d);
                }
            }

            return TriggerDetectors;
        }


        /* private IEnumerator Start()
         {
             yield return new WaitForSeconds(5f);
             RIGID_BODY.AddForce(200f * Vector3.up);
             yield return new WaitForSeconds(0.5f);
             TurnOnRagdoll();
         } */ 
        public void SetRagdollParts()
        {
            RagdollParts.Clear();
           
            Collider[] colliders = this.gameObject.GetComponentsInChildren<Collider>();

            foreach(Collider c in colliders)
            {
                if (c.gameObject != this.gameObject)
                {
                    c.isTrigger = true;
                    RagdollParts.Add(c);

                    if(c.GetComponent<TriggerDetector>() == null)
                    {
                        c.gameObject.AddComponent<TriggerDetector>();
                    }
                }
            }
        }
      
        public void TurnOnRagdoll()
        {
            RIGID_BODY.useGravity = false;
            RIGID_BODY.velocity = Vector3.zero;
            this.gameObject.GetComponent<BoxCollider>().enabled = false;
            SkinnedMeshAnimator.enabled = false;
            SkinnedMeshAnimator.avatar = null;

            foreach(Collider c in RagdollParts)
            {
                c.isTrigger = false;
                c.attachedRigidbody.velocity = Vector3.zero;
            }
        }

        private void SetColliderSpheres()
        {
            BoxCollider box = GetComponent<BoxCollider>();

            float bottom = box.bounds.center.y - box.bounds.extents.y;
            float top = box.bounds.center.y + box.bounds.extents.y;
            float front = box.bounds.center.z + box.bounds.extents.z;
            float back = box.bounds.center.z - box.bounds.extents.z;

            GameObject bottomFrontHor = CreateEdgeSphere(new Vector3(0f, bottom, front));
            GameObject bottomFrontVer = CreateEdgeSphere(new Vector3(0f, bottom + 0.05f, front));
            GameObject bottomBack = CreateEdgeSphere(new Vector3(0f, bottom, back));
            GameObject topFront = CreateEdgeSphere(new Vector3(0f, top, front));
            //GameObject topBottom = CreateEdgeSphere(new Vector3(0f, top, back));

            bottomFrontHor.transform.parent = this.transform;
            bottomFrontVer.transform.parent = this.transform;
            bottomBack.transform.parent = this.transform;
            topFront.transform.parent = this.transform;

            BottomSpheres.Add(bottomFrontHor);
            BottomSpheres.Add(bottomBack);

            FrontSpheres.Add(bottomFrontVer);
            FrontSpheres.Add(topFront);

            float horSec = (bottomFrontHor.transform.position - bottomBack.transform.position).magnitude / 5f;
            CreateMiddleSpheres(bottomFrontHor, -this.transform.forward, horSec, 4, BottomSpheres);

            float verSec = (bottomFrontVer.transform.position - topFront.transform.position).magnitude / 10f;
            CreateMiddleSpheres(bottomFrontVer, this.transform.up, verSec, 9, FrontSpheres);
        }

        private void FixedUpdate()
        {
            if(RIGID_BODY.velocity.y < 0f)
            {
                RIGID_BODY.velocity += (-Vector3.up * GravityMultiplier);
            }

            if (RIGID_BODY.velocity.y > 0f && !Jump)
            {
                RIGID_BODY.velocity += (-Vector3.up * PullMultiplier);
            }
        }

        public void CreateMiddleSpheres(GameObject start, Vector3 dir, float sec, int interations, List<GameObject> spheresList)
        {
            for (int i = 0; i < interations; i++)
            {
                Vector3 pos = start.transform.position + (dir * sec * (i + 1));

                GameObject newObj = CreateEdgeSphere(pos);
                newObj.transform.parent = this.transform;
                spheresList.Add(newObj);
            }
        }

        public GameObject CreateEdgeSphere(Vector3 pos)
        {
            GameObject obj = Instantiate(ColliderEdgePrefab, pos, Quaternion.identity);
            return obj;
        }

        public void MoveForward(float speed, float speedGraph)
        {
            transform.Translate(Vector3.forward * speed * speedGraph * Time.deltaTime);
        }

        public void FaceForward(bool forward)
        {
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Equals(SS_Scenes.CharacterSelect.ToString()))
            {
                return;
            }

            if(forward)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
        }

        public bool IsFacingForward()
        {
            if (transform.forward.z > 0f)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public Collider GetBodyPart(string name)
        {
            foreach(Collider c in RagdollParts)
            {
                if(c.name.Contains(name))
                {
                    return c;
                }
            }
            return null;
        }

        public GameObject GetChildObj(string name)
        {
            if(ChildObjects.ContainsKey(name))
            {
                return ChildObjects[name];
            }

            Transform[] arr = this.gameObject.GetComponentsInChildren<Transform>();

            foreach(Transform t in arr)
            {
                if(t.gameObject.name.Equals(name))
                {
                    ChildObjects.Add(name, t.gameObject);
                    return t.gameObject;
                }
            }

            return null;
        }

    }

}