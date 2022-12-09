using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class BrickSpawner : MonoBehaviour
{
    public SteamVR_Action_Boolean squeezeAction;
    public SteamVR_Action_Boolean triggerAction;

    public Hand.AttachmentFlags attachmentFlags = Hand.AttachmentFlags.ParentToHand | Hand.AttachmentFlags.DetachFromOtherHand | Hand.AttachmentFlags.TurnOnKinematic;


    [SerializeField] private PlacedObject brickToSpawn;
    [SerializeField] private Interactable interactable;
    // Start is called before the first frame update
    void Start()
    {
        //interactable = GetComponent<Interactable>();
    }

    // Update is called once per frame
    void Update()
    {
        bool pickupActionPerformed = triggerAction.GetStateDown(SteamVR_Input_Sources.Any) || squeezeAction.GetStateDown(SteamVR_Input_Sources.Any);
        if (interactable.isHovering && pickupActionPerformed)
        {
            Debug.Log("PICKUP!!!");
            Instantiate(brickToSpawn, transform.position, transform.rotation);
            brickToSpawn.transform.localScale =new Vector3(0.004f, 0.004f, 0.004f);
            Hand grabbingHand = interactable.hoveringHand;
            GrabTypes grabType = grabbingHand.GetBestGrabbingType();

            grabbingHand.AttachObject(brickToSpawn.gameObject, grabType, attachmentFlags);
        }
    }
}
