using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerActionsInput : MonoBehaviour
{
    [SerializeField] private bool jump;
    public bool Jump { get { return jump; } set { jump = value; } }
    [SerializeField] private bool openDoor;
    public bool OpenDoor { get { return openDoor; } set { openDoor = value; } }
    [SerializeField] private bool sprint;
    public bool Sprint { get { return sprint; } set { sprint = value; } }
    [SerializeField] private bool crounch;
    public bool Crounch { get { return crounch; } set { crounch = value; } }
    [SerializeField] private Vector2 move;
    public Vector2 Move => move;
    [SerializeField] private Vector2 look;
    public Vector2 Look => look;
    [SerializeField] private bool cursorLocked = true;
    public bool CursorLocked => cursorLocked;
    [SerializeField] private bool cursorInputForLook = true;
    public bool CursorInputForLook => cursorInputForLook;


    public void OnJump(InputValue value)
    {
        JumpInput(value.isPressed);
    }
    public void OnOpenDoor(InputValue value)
    {
        OpenDoorInput(value.isPressed);
    }
    public void OnSprint(InputValue value)
    {
        SprintInput(value.isPressed);
    }
    public void OnCrounch(InputValue value)
    {
        CrounchInput(value.isPressed);
    }
    public void OnMove(InputValue value)
    {
        MoveInput(value.Get<Vector2>());
    }
    public void OnLook(InputValue value)
    {
        if (cursorInputForLook)
        {
            LookInput(value.Get<Vector2>());
        }
    }

    public void JumpInput(bool jumpState)
    {
        jump = jumpState;
    }
    public void OpenDoorInput(bool openState)
    {
        openDoor = openState;
    }
    public void SprintInput(bool sprintState)
    {
        sprint = sprintState;
    }
    public void CrounchInput(bool crounchState)
    {
        crounch = crounchState;
    }
    private void MoveInput(Vector2 moveDirection)
    {
        move = moveDirection;
    }
    private void LookInput(Vector2 lookDirection)
    {
        look = lookDirection;
    }
    private void OnApplicationFocus(bool hasFocus)
    {
        SetCursorState(cursorLocked);
    }

    private void SetCursorState(bool newState)
    {
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
