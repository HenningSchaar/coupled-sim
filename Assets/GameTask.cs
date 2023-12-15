using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTask : MonoBehaviour
{
    GameObject cylinderRef;
    GameObject ringRef;
    GameObject scoreUIRef;
    Renderer ringRenderer;
    private bool learn = false;
    private string path;
    private string logString;
    public int score;
    public int displayScore;
    private bool insideOfRing;
    private bool m_insideOfRing;
    private float cylinderSpeed = 0.0005f;
    public Vector3 cylinderDirection = new Vector3(0,0,0);
    private float ringSpeed = 0.0015f;
    public float timeLeft;
    public float accelerationTime = 2f;
    private float horizontalDisplayBoundaryCylinder = 0.095f;
    private float verticalDisplayBoundaryCylinder = 0.065f;
    private float positionDelta = 0.03f;
    private float cylinderXPos;
    private float cylinderYPos;
    public float ringXPos
    {
        get { return _ringXPos; }
        set { _ringXPos = Mathf.Clamp(value, -0.092f, 0.092f); }
    }
    [SerializeField, Range(-0.092f, 0.092f)] private float _ringXPos;
    public float ringYPos
    {
        get { return _ringYPos; }
        set { _ringYPos = Mathf.Clamp(value, -0.06f, 0.06f); }
    }
    [SerializeField, Range(-0.06f, 0.06f)] private float _ringYPos;
    private float horizontalInput;
    private float verticalInput;

    bool checkBounds(GameObject Object, float horizontalBoundary, float verticalBoundary)
    {
      float x = Object.transform.localPosition[0];
      float y = Object.transform.localPosition[1];
      if (x <= -verticalBoundary || x >= verticalBoundary)
      {
        return true;
      }
      if (y <= -horizontalBoundary || y >= horizontalBoundary)
      {
        return true;
      }
      return false;
    }

    bool checkInRing()
    {
      //Debug.Log(ringXPos + " " + ringYPos + " " + cylinderXPos + " " + cylinderYPos);
      if (ringXPos - positionDelta <= cylinderXPos && cylinderXPos <= ringXPos + positionDelta && ringYPos - positionDelta <= cylinderYPos && cylinderYPos <= ringYPos + positionDelta)
      {
        changeRingColor("green");
        return true;
      }
      else
      {
        changeRingColor("red");
        return false;
      }
    }

    void changeRingColor(string color)
    {
      if (color == "red")
      {
        ringRef.GetComponent<Renderer>().material.SetColor("_Color", Color.red);
      }
      if (color == "green")
      {
        ringRef.GetComponent<Renderer>().material.SetColor("_Color", Color.green);
      }
    }
    int calculateDisplayScore(int score)
    {
      displayScore = Mathf.RoundToInt(score/10);
      return displayScore;
    }
    void CreateLog(string logText)
    {
      string timestamp = System.DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss");
      PlayerPrefs.SetString("timestamp", timestamp);
      if (System.String.IsNullOrEmpty(path))
      {
        path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/" + "log_" + timestamp + ".txt";
      }
      // This text is added only once to the file.
      if (!System.IO.File.Exists(path)) {
         // Create a file to write to.
         using (System.IO.StreamWriter sw = System.IO.File.CreateText(path))
         {
             sw.WriteLine(System.DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss") + ": " + "App initialised");
         }
      } else {
         // This text is always added, making the file longer over time
         // if it is not deleted.
         using (System.IO.StreamWriter sw = System.IO.File.AppendText(path)) {
             sw.WriteLine(logText);
         }
      }
    }
    // Start is called before the first frame update
    void Start()
    {
      cylinderRef = GameObject.Find("iPadDisplay").transform.GetChild(0).gameObject;
      ringRef = GameObject.Find("iPadDisplay").transform.GetChild(1).gameObject;
      scoreUIRef = GameObject.Find("iPadDisplay").transform.GetChild(2).gameObject;
      if (GameObject.Find ("Learn(Clone)") != null) {
             learn = true;
             Debug.Log("Learn enabled");
         }else{
           Debug.Log("Learn disabled");
         }
    }

    void OnApplicationQuit()
    {
      CreateLog(Time.realtimeSinceStartup.ToString() + " " + insideOfRing.ToString());
      CreateLog("Experiment ended after " + Time.realtimeSinceStartup.ToString() + " with a score of:\r\n" + score);
    }

    // Update is called once per frame
    void Update()
    {
      // Get controller Data
      horizontalInput = Input.GetAxis("Horizontal");
      verticalInput = Input.GetAxis("Vertical");
    }

    void FixedUpdate()
    {
      // Calculate new Direction vector after accelerationTime has passed
      timeLeft -= 0.01f;
      if (timeLeft <= 0 && !checkBounds(cylinderRef, horizontalDisplayBoundaryCylinder, verticalDisplayBoundaryCylinder))
      {
        cylinderDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
        //Debug.Log(cylinderDirection);
        timeLeft += accelerationTime;
      }
      // Movement of Cylinder
      if (checkBounds(cylinderRef, horizontalDisplayBoundaryCylinder, verticalDisplayBoundaryCylinder) == false)
      {
        cylinderRef.transform.Translate(cylinderDirection * cylinderSpeed);
      }
      else
      {
        cylinderDirection = cylinderDirection * -1f;
        cylinderRef.transform.Translate(cylinderDirection * cylinderSpeed);
      }

      // Movement of Ring
      // ringRef.transform.Translate(new Vector3(0f, horizontalInput * ringSpeed, verticalInput * ringSpeed));
      ringXPos += horizontalInput * ringSpeed;
      ringYPos += verticalInput * ringSpeed;
      ringRef.transform.localPosition = new Vector3( ringYPos, ringXPos, 0f);
      cylinderYPos = cylinderRef.transform.localPosition[0];
      cylinderXPos = cylinderRef.transform.localPosition[1];
      // Scoring & logging
      if (checkInRing()){
        score += 1;
        insideOfRing = true;
        logString = Time.realtimeSinceStartup.ToString() + " " + insideOfRing.ToString();

      }
      else
      {
        insideOfRing = false;
        logString = Time.realtimeSinceStartup.ToString() + " " + insideOfRing.ToString();
        if(score >= 1)
        {
          score += 1;
        }
      }
      if(m_insideOfRing != insideOfRing && learn == false)
      {
        CreateLog(logString);
      }
      displayScore = calculateDisplayScore(score);
      scoreUIRef.GetComponent<UnityEngine.UI.Text>().text = displayScore.ToString();
      m_insideOfRing = insideOfRing;
    }
}
