using System;
using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static int killstreak;

    public float speed = 0.4f;

    public PointSprites points;

    private bool _deadPlaying;
    private Vector2 _dest = Vector2.zero;
    private Vector2 _dir = Vector2.zero;
    private Vector2 _nextDir = Vector2.zero;
    private GameManager GM;

    // script handles
    private GameGUINavigation GUINav;
    private ScoreManager SM;

    // Use this for initialization
    private void Start()
    {
        GM = GameObject.Find("Game Manager").GetComponent<GameManager>();
        SM = GameObject.Find("Game Manager").GetComponent<ScoreManager>();
        GUINav = GameObject.Find("UI Manager").GetComponent<GameGUINavigation>();
        _dest = transform.position;
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        switch (GameManager.gameState)
        {
            case GameManager.GameState.Game:
                ReadInputAndMove();
                Animate();
                break;

            case GameManager.GameState.Dead:
                if (!_deadPlaying)
                    StartCoroutine("PlayDeadAnimation");
                break;
        }
    }

    private IEnumerator PlayDeadAnimation()
    {
        _deadPlaying = true;
        GetComponent<Animator>().SetBool("Die", true);
        yield return new WaitForSeconds(1);
        GetComponent<Animator>().SetBool("Die", false);
        
        yield return new WaitForSeconds(2.0f);
        _deadPlaying = false;

        if (GameManager.lives <= 0)
        {
            Debug.Log("Treshold for High Score: " + SM.LowestHigh());
            if (GameManager.score >= SM.LowestHigh())
                GUINav.getScoresMenu();
            else
                GUINav.H_ShowGameOverScreen();
        }

        else
        {
            GM.ResetScene();
        }
    }

    private void Animate()
    {
        var dir = _dest - (Vector2) transform.position;
        GetComponent<Animator>().SetFloat("DirX", dir.x);
        GetComponent<Animator>().SetFloat("DirY", dir.y);
    }

    private bool Valid(Vector2 direction)
    {
        // cast line from 'next to pacman' to pacman
        // not from directly the center of next tile but just a little further from center of next tile
        Vector2 pos = transform.position;
        direction += new Vector2(direction.x * 0.45f, direction.y * 0.45f);
        var hit = Physics2D.Linecast(pos + direction, pos);
        return hit.collider.name == "pacdot" || hit.collider == GetComponent<Collider2D>();
    }

    public void ResetDestination()
    {
        _dest = new Vector2(15f, 11f);
        GetComponent<Animator>().SetFloat("DirX", 1);
        GetComponent<Animator>().SetFloat("DirY", 0);
    }

    private void ReadInputAndMove()
    {
        // move closer to destination
        var p = Vector2.MoveTowards(transform.position, _dest, speed);
        GetComponent<Rigidbody2D>().MovePosition(p);

        // get the next direction from keyboard
        if (Input.GetAxis("Horizontal") > 0) _nextDir = Vector2.right;
        if (Input.GetAxis("Horizontal") < 0) _nextDir = -Vector2.right;
        if (Input.GetAxis("Vertical") > 0) _nextDir = Vector2.up;
        if (Input.GetAxis("Vertical") < 0) _nextDir = -Vector2.up;

        // if pacman is in the center of a tile
        if (Vector2.Distance(_dest, transform.position) < 0.00001f)
        {
            if (Valid(_nextDir))
            {
                _dest = (Vector2) transform.position + _nextDir;
                _dir = _nextDir;
            }
            else // if next direction is not valid
            {
                if (Valid(_dir)) // and the prev. direction is valid
                    _dest = (Vector2) transform.position + _dir; // continue on that direction

                // otherwise, do nothing
            }
        }
    }

    public Vector2 getDir()
    {
        return _dir;
    }

    public void UpdateScore()
    {
        killstreak++;

        // limit killstreak at 4
        if (killstreak > 4) killstreak = 4;

        Instantiate(points.pointSprites[killstreak - 1], transform.position, Quaternion.identity);
        GameManager.score += (int) Mathf.Pow(2, killstreak) * 100;
    }

    [Serializable]
    public class PointSprites
    {
        public GameObject[] pointSprites;
    }
}