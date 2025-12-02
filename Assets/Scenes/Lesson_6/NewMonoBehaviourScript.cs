using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
//Base Value-Type Variable
    public int MyInt = 5;
    public float MyFloat = 5.9393;
    public string MyString = "HelloWorld";
    public bool MyBool = true;

//Composite Value-Type Variable
    public Vector2 myVector2 = new Vector (1.124f, 2.45f);
    public Vector3 myVector3 = new Vector (1.124f, 2.45f, 3.45f);
    public Color myColor = new Color (r: 1.124f, g: 0.45f, b: 0.789f, a: 1f);

    //Reference-Type Variables
    public GameObject MyGameObject;
    public Camera MyCamera;
    public Material MyMaterial;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        Debug.Log "Start method executed";

        MyInt = 10;
        MyFloat = 10.9393f;
        MyString = "HelloUnity";
        MyBool = false;

        Vector2 myVector2 = new Vector (1.124f, 2.45f);
        Vector3 myVector3 = new Vector (1.124f, 2.45f, 3.45f);
        Color myColor = new Color (r: 1.124f, g: 0.45f, b: 0.789f, a: 1f);

        MyCamera = Camera.main;
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
