using Unity.Netcode;
using UnityEngine;

public class Mover : NetworkBehaviour
{
    private Vector2 moveVector;
    private bool isMoving;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

        float moveVectorX = Input.GetAxis("Horizontal");
        float moveVectoryY = Input.GetAxis("Vertical");

        moveVector = new Vector2(moveVectorX, moveVectoryY);
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveVector;
    }
}
