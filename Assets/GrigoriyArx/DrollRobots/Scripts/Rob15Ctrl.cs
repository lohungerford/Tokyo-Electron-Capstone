using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rob15Ctrl : MonoBehaviour
{
    public float speed = 1.0f;
    public float run = 0;
    public float velocity = 0;
    float runVelocity = 1f;

    public GameObject MouthEmo, MouthSpeech;

    public RobotColorManager robotColorManager;
    public EmotionChanger emotionChanger;
    public PushDetection pushDetection;

    [Header("Repeat time for some animations")]
    public int playCount = 1; // Cyclyc Animations repeat time
    private int currentPlayCount = 0;

    private int currentNumber = 0; //
    int N = 2;             

    private string animationName = "YourAnimationName";
    private bool battleIsActive = false;
    private bool isPushing = false;
    public string pushableTag = "Pushable";


    int emo_i = 0;

    Animator anim;
    CharacterController controller;
    /*
     * Emotions list with ID
         0.Neutral
         1.Happy
         2.Sad
         3.Distrust
         4.Wonder
         5.Death
         6.Disgust
         7.Evil
         8.Cry
         9.Love
     */

    public int GetNextNumber(int N)
    {
        int result = currentNumber;
        currentNumber = (currentNumber + 1) % (N + 1); // Increase and reset if exceeds N
        Debug.Log(result);
        return result;
    }

    void Start()
    {
        anim = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        anim.SetFloat("speedMultiplier", speed);


    }

    void Update()
    {
        anim.SetFloat("Side", Input.GetAxis("Horizontal"));
        anim.SetFloat("Speed", Input.GetAxis("Vertical"));

        if (Input.GetKey(KeyCode.LeftShift) && (run < 1))
        {

            run += Time.deltaTime * runVelocity;
            anim.SetFloat("run", run);
        }
        else
        {
            if (run > 0)
            {
                run -= Time.deltaTime * runVelocity;
            }
            anim.SetFloat("run", run);
        }

        if (pushDetection != null && pushDetection.isPushing)
        {
            if (Input.GetAxis("Vertical")>0.1)
            {
                Debug.Log("Персонаж толкает объект: ");
                anim.SetBool("Push", true);
                setEmotion(6);
            }
            if (Input.GetAxis("Vertical") < 0.1)
            {
                Debug.Log("Персонаж толкает объект: ");
                anim.SetBool("Push", false);
                resetEmo();
            }
        }

        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            emo_i++;
            if (emo_i == 10) { emo_i = 0; }
            setEmotion(emo_i);
        }
        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            emo_i--;
            if (emo_i == 0) { emo_i = 10; }
            setEmotion(emo_i - 1);
        }

        //------------BATTLE-----------------
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            battleIsActive = !battleIsActive;
            if (battleIsActive) { setEmotion(7);}
            if (!battleIsActive) {resetEmo();}
            Debug.Log("battleIsActive=" + battleIsActive);
            anim.SetBool("Battle", battleIsActive);

        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            anim.SetBool("Block", true);
        }
        if (Input.GetKeyUp(KeyCode.B))
        {
            anim.SetBool("Block", false);
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            anim.SetBool("Jump", true);
            anim.SetInteger("vary", GetNextNumber(2));
        }
        if (Input.GetKeyDown(KeyCode.U))
        {
            anim.SetBool("FallFront", true);
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            anim.SetBool("FallBack", true);
        }

        //------------EMOTIONS-----------------

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            resetEmo();

        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            anim.SetBool("Angry", true);
            setEmotion(2);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            anim.SetBool("Cry", true);
            setEmotion(8);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            anim.SetBool("Thumb", true);
            setEmotion(9);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            anim.SetBool("Win", true);
            setEmotion(1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            anim.SetBool("DontKnow", true);
            setEmotion(3);
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            anim.SetBool("Hello", true);
            setEmotion(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            anim.SetBool("Laught", true);
            setEmotion(1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            anim.SetBool("LookingFor", true);
            setEmotion(4);
        }

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            animationName = "Dance0";
            robotColorManager.isRainbowCycles = true;
            setEmotion(1);
            StartCoroutine(PlayAnimationMultipleTimes());
        }
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            animationName = "Dance1";
            robotColorManager.isRainbowCycles = true;
            setEmotion(1);
            StartCoroutine(PlayAnimationMultipleTimes());
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            anim.SetBool("Talk", true);
            ToggleObjectActiveState();
            setEmotion(0);
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            anim.SetBool("Pointing", true);
            //anim.SetInteger("vary", Random.Range(0, 2));//random value
            anim.SetInteger("vary", GetNextNumber(3));
            setEmotion(0);
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            anim.SetBool("Hit", true);
            // anim.SetInteger("vary", Random.Range(0, 2));  //random value
            anim.SetInteger("vary", GetNextNumber(3));
            setEmotion(0);
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            anim.SetBool("StrafeLeft", true);
        }
        if (Input.GetKeyUp(KeyCode.Q))
        {
            anim.SetBool("StrafeLeft", false);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            anim.SetBool("StrafeRight", true);
        }
        if (Input.GetKeyUp(KeyCode.E))
        {
            anim.SetBool("StrafeRight", false);
        }


    }

    public void setEmotion(int emoNumber)
    {
        if (battleIsActive)
        {
            emoNumber = 7;
        }
            robotColorManager.ChangeBodyColor(emoNumber);
            emotionChanger.SetEmotionEyes(emoNumber);
            emotionChanger.SetEmotionMouth(emoNumber);
    }

    public void Speech3End()
    {
        ToggleObjectActiveState();
        Debug.Log("Anitions is ended!");
    }

    IEnumerator PlayAnimationMultipleTimes()
    {
        for (int i = 0; i < playCount; i++)
        {
            anim.SetBool(animationName, true);
            yield return new WaitForSeconds(playCount);
        }
        anim.SetBool(animationName, false);
        robotColorManager.isRainbowCycles = false;
        anim.SetBool("reset", true);
        resetEmo();
        Debug.Log("Animation Done");
    }

    void ToggleObjectActiveState()
    {
        if ((MouthEmo != null) && (MouthSpeech != null))
        {
            bool isActive = MouthEmo.activeSelf;  
            bool isActiveS = MouthSpeech.activeSelf;
            MouthEmo.SetActive(!isActive); 
            MouthSpeech.SetActive(!isActiveS);
        }
        else
        {
            Debug.LogError("Target Object не назначен!");
        }
    }

    void resetEmo()
    {
        setEmotion(0);
        anim.SetBool("reset", true);
    }

}

