using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public bool moveLeft { get; private set; }
    public bool moveRight { get; private set; }
    public bool jump { get; private set; }
    public bool slide { get; private set; }

    void Start()
    {
        moveLeft = false;
        moveRight = false;
        jump = false;
        slide = false;
    }

    void Update()
    {
        // Windows Input
        moveLeft = Input.GetKeyDown(KeyCode.LeftArrow);
        moveRight = Input.GetKeyDown(KeyCode.RightArrow);
        jump = Input.GetKeyDown(KeyCode.UpArrow);
        slide = Input.GetKeyDown(KeyCode.DownArrow);
    }
}
