using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GhostMove : MonoBehaviour
{
    // direction is set from the AI component
    public Vector3 _direction;

    public float speed = 0.3f;

    // ----------------------------
    // Ghost mode variables
    public float scatterLength = 5f;
    public float waitLength;

    // handles
    public GameGUINavigation GUINav;
    public PlayerController pacman;

    public float DISTANCE;
    private GameManager _gm;

    private Vector3 _startPos;
    private float _timeToToggleWhite;
    private float _timeToWhite;
    private float _toggleInterval;
    private bool isWhite;
    private State state;

    private float timeToEndScatter;
    private float timeToEndWait;

    // ----------------------------
    // Navigation variables
    private Vector3 waypoint; // AI-determined waypoint
    private Queue<Vector3> waypoints; // waypoints used on Init and Scatter states

    public Vector3 direction
    {
        get => _direction;

        set
        {
            _direction = value;
            var pos = new Vector3((int) transform.position.x, (int) transform.position.y, (int) transform.position.z);
            waypoint = pos + _direction;
            //Debug.Log ("waypoint (" + waypoint.position.x + ", " + waypoint.position.y + ") set! _direction: " + _direction.x + ", " + _direction.y);
        }
    }

    //-----------------------------------------------------------------------------------------
    // variables end, functions begin
    private void Start()
    {
        _gm = GameObject.Find("Game Manager").GetComponent<GameManager>();
        _toggleInterval = _gm.scareLength * 0.33f * 0.20f;
        InitializeGhost();
    }

    private void FixedUpdate()
    {
        DISTANCE = Vector3.Distance(transform.position, waypoint);

        if (GameManager.gameState == GameManager.GameState.Game)
        {
            animate();

            switch (state)
            {
                case State.Wait:
                    Wait();
                    break;

                case State.Init:
                    Init();
                    break;

                case State.Scatter:
                    Scatter();
                    break;

                case State.Chase:
                    ChaseAI();
                    break;

                case State.Run:
                    RunAway();
                    break;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.name == "pacman")
        {
            //Destroy(other.gameObject);
            if (state == State.Run)
            {
                Calm();
                InitializeGhost(_startPos);
                pacman.UpdateScore();
            }

            else
            {
                _gm.LoseLife();
            }
        }
    }

    //-----------------------------------------------------------------------------------
    // Start() functions

    public void InitializeGhost()
    {
        _startPos = getStartPosAccordingToName();
        waypoint = transform.position; // to avoid flickering animation
        state = State.Wait;
        timeToEndWait = Time.time + waitLength + GUINav.initialDelay;
        InitializeWaypoints(state);
    }

    public void InitializeGhost(Vector3 pos)
    {
        transform.position = pos;
        waypoint = transform.position; // to avoid flickering animation
        state = State.Wait;
        timeToEndWait = Time.time + waitLength + GUINav.initialDelay;
        InitializeWaypoints(state);
    }


    private void InitializeWaypoints(State st)
    {
        //--------------------------------------------------
        // File Format: Init and Scatter coordinates separated by empty line
        // Init X,Y 
        // Init X,Y
        // 
        // Scatter X,Y
        // Scatter X,Y

        //--------------------------------------------------
        // hardcode waypoints according to name.
        var data = "";
        switch (name)
        {
            case "blinky":
                data = @"22 20
22 26

27 26
27 30
22 30
22 26";
                break;
            case "pinky":
                data = @"14.5 17
14 17
14 20
7 20

7 26
7 30
2 30
2 26";
                break;
            case "inky":
                data = @"16.5 17
15 17
15 20
22 20

22 8
19 8
19 5
16 5
16 2
27 2
27 5
22 5";
                break;
            case "clyde":
                data = @"12.5 17
14 17
14 20
7 20

7 8
7 5
2 5
2 2
13 2
13 5
10 5
10 8";
                break;
        }

        //-------------------------------------------------
        // read from the hardcoded waypoints
        string line;

        waypoints = new Queue<Vector3>();
        Vector3 wp;

        if (st == State.Init)
            using (var reader = new StringReader(data))
            {
                // stop reading if empty line is reached
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length == 0) break; // DOES IT WORK????

                    var values = line.Split(' ');
                    var x = float.Parse(values[0]);
                    var y = float.Parse(values[1]);

                    wp = new Vector3(x, y, 0);
                    waypoints.Enqueue(wp);
                }
            }

        if (st == State.Scatter)
        {
            // skip until empty line is reached, read coordinates afterwards
            var scatterWps = false; // Scatter waypoints

            using (var reader = new StringReader(data))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length == 0)
                    {
                        scatterWps = true; // we reached the scatter waypoints
                        continue; // do not read empty line, go to next line
                    }

                    if (scatterWps)
                    {
                        var values = line.Split(' ');
                        var x = int.Parse(values[0]);
                        var y = int.Parse(values[1]);

                        wp = new Vector3(x, y, 0);
                        waypoints.Enqueue(wp);
                    }
                }
            }
        }

        // if in wait state, patrol vertically
        if (st == State.Wait)
        {
            var pos = transform.position;

            // inky and clyde start going down and then up
            if (transform.name == "inky" || transform.name == "clyde")
            {
                waypoints.Enqueue(new Vector3(pos.x, pos.y - 0.5f, 0f));
                waypoints.Enqueue(new Vector3(pos.x, pos.y + 0.5f, 0f));
            }
            // while pinky start going up and then down
            else
            {
                waypoints.Enqueue(new Vector3(pos.x, pos.y + 0.5f, 0f));
                waypoints.Enqueue(new Vector3(pos.x, pos.y - 0.5f, 0f));
            }
        }
    }

    private Vector3 getStartPosAccordingToName()
    {
        switch (gameObject.name)
        {
            case "blinky":
                return new Vector3(15f, 20f, 0f);

            case "pinky":
                return new Vector3(14.5f, 17f, 0f);

            case "inky":
                return new Vector3(16.5f, 17f, 0f);

            case "clyde":
                return new Vector3(12.5f, 17f, 0f);
        }

        return new Vector3();
    }

    //------------------------------------------------------------------------------------
    // Update functions
    private void animate()
    {
        var dir = waypoint - transform.position;
        GetComponent<Animator>().SetFloat("DirX", dir.x);
        GetComponent<Animator>().SetFloat("DirY", dir.y);
        GetComponent<Animator>().SetBool("Run", false);
    }

    //-----------------------------------------------------------------------------------
    // State functions
    private void Wait()
    {
        if (Time.time >= timeToEndWait)
        {
            state = State.Init;
            waypoints.Clear();
            InitializeWaypoints(state);
        }
        
        //Runs when the animator is off
        if (GetComponent<Animator>().isActiveAndEnabled == false)
        {
            GetComponent<SpriteRenderer>().color = Color.white;
        }

        // get the next waypoint and move towards it
        MoveToWaypoint(true);
    }

    private void Init()
    {
        _timeToWhite = 0;

        //Runs when the animator is off
        if (GetComponent<Animator>().isActiveAndEnabled == false)
        {
            GetComponent<SpriteRenderer>().color = Color.white;
        }
        
        // if the Queue is cleared, do some clean up and change the state
        if (waypoints.Count == 0)
        {
            state = State.Scatter;

            //get direction according to sprite name
            var name = GetComponent<SpriteRenderer>().sprite.name;
            if (name[name.Length - 1] == '0' || name[name.Length - 1] == '1') direction = Vector3.right;
            if (name[name.Length - 1] == '2' || name[name.Length - 1] == '3') direction = Vector3.left;
            if (name[name.Length - 1] == '4' || name[name.Length - 1] == '5') direction = Vector3.up;
            if (name[name.Length - 1] == '6' || name[name.Length - 1] == '7') direction = Vector3.down;

            InitializeWaypoints(state);
            timeToEndScatter = Time.time + scatterLength;

            return;
        }

        // get the next waypoint and move towards it
        MoveToWaypoint();
    }

    private void Scatter()
    {
        if (Time.time >= timeToEndScatter)
        {
            waypoints.Clear();
            state = State.Chase;
            return;
        }
        
        //Runs when the animator is off
        if (GetComponent<Animator>().isActiveAndEnabled == false)
        {
            GetComponent<SpriteRenderer>().color = Color.white;
        }

        // get the next waypoint and move towards it
        MoveToWaypoint(true);
    }

    private void ChaseAI()
    {
        // if not at waypoint, move towards it
        if (Vector3.Distance(transform.position, waypoint) > 0.000000000001)
        {
            var p = Vector2.MoveTowards(transform.position, waypoint, speed);
            GetComponent<Rigidbody2D>().MovePosition(p);
        }

        // if at waypoint, run AI module
        else
        {
            GetComponent<AI>().AILogic();
        }
        
        //Runs when the animator is off
        if (GetComponent<Animator>().isActiveAndEnabled == false)
        {
            GetComponent<SpriteRenderer>().color = Color.white;
        }
    }

    private void RunAway()
    {
        GetComponent<Animator>().SetBool("Run", true);

        if (Time.time >= _timeToWhite && Time.time >= _timeToToggleWhite) ToggleBlueWhite();

        //Runs when the animator is off
        if (GetComponent<Animator>().isActiveAndEnabled == false)
        {
            GetComponent<SpriteRenderer>().color = Color.red;
        }

        // if not at waypoint, move towards it
        if (Vector3.Distance(transform.position, waypoint) > 0.000000000001)
        {
            var p = Vector2.MoveTowards(transform.position, waypoint, speed);
            GetComponent<Rigidbody2D>().MovePosition(p);
        }

        // if at waypoint, run AI run away logic
        else
        {
            GetComponent<AI>().RunLogic();
        }
    }

    //------------------------------------------------------------------------------
    // Utility functions
    private void MoveToWaypoint(bool loop = false)
    {
        waypoint = waypoints.Peek(); // get the waypoint (CHECK NULL?)
        if (Vector3.Distance(transform.position, waypoint) > 0.000000000001) // if its not reached
        {
            // move towards it
            _direction =
                Vector3.Normalize(waypoint - transform.position); // dont screw up waypoint by calling public setter
            var p = Vector2.MoveTowards(transform.position, waypoint, speed);
            GetComponent<Rigidbody2D>().MovePosition(p);
        }
        else // if waypoint is reached, remove it from the queue
        {
            if (loop) waypoints.Enqueue(waypoints.Dequeue());
            else waypoints.Dequeue();
        }
    }

    public void Frighten()
    {
        state = State.Run;
        _direction *= -1;

        _timeToWhite = Time.time + _gm.scareLength * 0.66f;
        _timeToToggleWhite = _timeToWhite;
        GetComponent<Animator>().SetBool("Run_White", false);
    }

    public void Calm()
    {
        // if the ghost is not running, do nothing
        if (state != State.Run) return;

        waypoints.Clear();
        state = State.Chase;
        _timeToToggleWhite = 0;
        _timeToWhite = 0;
        GetComponent<Animator>().SetBool("Run_White", false);
        GetComponent<Animator>().SetBool("Run", false);
    }

    public void ToggleBlueWhite()
    {
        isWhite = !isWhite;
        GetComponent<Animator>().SetBool("Run_White", isWhite);
        _timeToToggleWhite = Time.time + _toggleInterval;
    }

    private enum State
    {
        Wait,
        Init,
        Scatter,
        Chase,
        Run
    }
}