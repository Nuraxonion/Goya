using UnityEngine;

public class CharacterMovement : MonoBehaviour {

    [SerializeField] float moveSpeed = 5f;

    void Update() {
        float x = Input.GetAxisRaw("Horizontal");   // A/D or Left/Right
        float z = Input.GetAxisRaw("Vertical");     // W/S or Up/Down

        Vector3 move = new Vector3(x, 0f, z).normalized;
        transform.position += move * moveSpeed * Time.deltaTime;
    }
}
