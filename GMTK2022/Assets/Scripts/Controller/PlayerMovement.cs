using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] public float PlayerSpeed = 5f;
    [SerializeField] private float gravityValue = -9.81f;
    [SerializeField] private float controllerDeadZone = 0.1f;
    [SerializeField] private float gamepadRoateSmoothing = -1000f;

    private CharacterController controller;

    public Transform debugDistance;

    public Vector2 movement;
    private Vector2 aim;

    // 延45度角移动
    private Quaternion moveQuaternion = Quaternion.AngleAxis(45f, Vector3.up);

    private Vector3 playerVelocity;
    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void HandleInput()
    {
        movement = new Vector3(Input.GetAxisRaw("Vertical"), Input.GetAxisRaw("Horizontal"));
        //Debug.Log(movement);
    }

    void HandleMovement()
    {
        Vector3 move = moveQuaternion * new Vector3(movement.x, 0, -movement.y);
        controller.Move(move * Time.deltaTime * PlayerSpeed);
    }
    void HandleRoation()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float rayDistance;
        if (groundPlane.Raycast(ray, out rayDistance))
        {
            Vector3 point = ray.GetPoint(rayDistance);

            transform.LookAt(new Vector3(point.x, transform.position.y, point.z));
        }
    }


    // Update is called once per frame
    void Update()
    {
        HandleInput();
        HandleMovement();
        HandleRoation();
        //Debug.Log("玩家距离debugpos距离"+Vector3.Distance(transform.position, debugDistance.transform.position));
    }
}
