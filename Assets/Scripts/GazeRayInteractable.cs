using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GazeRayInteractable : XRBaseInteractable {

    public MeshRenderer meshRenderer;
    public Material materialOnSelect;

    public Material materialByDefault;

    public void onSelectEntered() {

    }
    
}
// {

//     public MeshRenderer meshRenderer;
//     public Material materialOnSelect;
//     public Material materialByDefault;

//     public override void OnSelectEntered(XRInteractableEvent event) {

//         meshRenderer.material = materialOnSelect;
//     }

//     public void onSelectExited() {
//         meshRenderer.material = materialByDefault;
//     }
// }
