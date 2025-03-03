using System;
using System.Collections;
using System.Collections.Generic;
using Gameplay;
using Teams;
using UI;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Utils;
using Random = UnityEngine.Random;

public enum QuaffleState
{
    Space,
    CachedByTeam1,
    CachedByTeam2,
}

public enum GameOverType
{
    TIME_UP,
    SNITCH_CAUGHT
}

public class GameManager : SingletonBehaviour<GameManager>
{
    [SerializeField]
    private PlayerType _playerStartType = PlayerType.Seeker;
    [SerializeField]
    private int gameTimeMinutes = 8;
    [SerializeField]
    private int gameStartCountdown = 3;
    [SerializeField]
    private Vector3 quaffleResetPosition = new Vector3(0, 100.0f, 0);
    [SerializeField]
    private Vector3 minSpacePoint = new Vector3(-100, 0, -100);
    [SerializeField]
    private Vector3 maxSpacePoint = new Vector3(100, 300, 100);
    [SerializeField]
    private float forceDistance = 20.0f;
    [SerializeField]
    public AudienceManager audienceManager;
    

    [Header("Testing only"),SerializeField]
    private float _gameTimeScale = 2;


    [Header("Testing only"), SerializeField]
    private bool _gameEnableSides = true;
    
    public QuaffleState g_quaffleState = QuaffleState.Space;
    public GameObject quaffle = null;
    public List<GameObject> Bludges = new List<GameObject>();
    public GameObject goldenSnitch = null;
    
    private Team _playerTeam;
    private float timerSeconds;

    public Action<Team> OnGoldenSnitchScored;
    public Action<Team> OnQuaffleScored;
    public Action<TimeSpan> OnTimerUpdate;
    public Action OnTeamsAssigned;
    public Action OnHalfTime;
    public Action<GameOverType> OnGameOver;

    private bool _halftimeDone = false;
    private bool _canPause = true;
    
    // Indicates if Game Started;
    public bool GameStarted { get; private set; }

    public Team PlayerTeam => _playerTeam;
    
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Time.timeScale = _gameTimeScale;
        _halftimeDone = false;
        StartGame();

        quaffle = GameObject.FindGameObjectWithTag("Quaffle");
        goldenSnitch = GameObject.FindGameObjectWithTag("GoldenSnitch");
        GameObject[] bludgers = GameObject.FindGameObjectsWithTag("Bludger");
        for(int i =0; i < bludgers.Length; i++)
        {
            Bludges.Add(bludgers[i]);
        }

        OnTimerUpdate += OnTimerUpdated;
        AudioManager.Instance.SetupMatchAudio();
    }

    private void OnTimerUpdated(TimeSpan span)
    {
        goldenSnitch.GetComponent<GoldenSnich>().UpdateVelocity(span.Seconds, ((gameTimeMinutes * 60) / 2));
    }

    private void Update()
    {
        CheckQuaffleState();
        if (Input.GetKeyDown(KeyCode.Escape) && _canPause && GameStarted)
        {
            _canPause = false;
            GameStarted = false;
            Time.timeScale = 0;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            GameUI.Instance.OnPause(() =>
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                
                Time.timeScale = 1;
                _canPause = true;
                GameStarted = true;
            });
        }
    }

    public void CheckQuaffleState()
    {
        if (quaffle != null)
        {
            if (quaffle.GetComponent<Quaffle>().isCached)
            {
                if (quaffle.GetComponent<Quaffle>().takenChaser.GetComponent<TeamEntity>().MyTeam == Team.Team_1)
                    g_quaffleState = QuaffleState.CachedByTeam1;
                else
                    g_quaffleState = QuaffleState.CachedByTeam2;
            }
            else
                g_quaffleState = QuaffleState.Space;
        }
    }

    public void StartGame()
    {
        timerSeconds = gameTimeMinutes * 60;
        var enumValues = new List<Team>((Team[])Enum.GetValues(typeof(Team)));
        enumValues.Remove(0);
        _playerTeam = enumValues[Random.Range(0, enumValues.Count)];
        
        var side1team = enumValues[Random.Range(0, enumValues.Count)];
        enumValues.Remove(side1team);
        var side2team = enumValues[0];
        
        if(_gameEnableSides)
        {
            SidesManager.Instance?.AssignTeams(side1team, side1team == _playerTeam,
                    side2team, side2team == _playerTeam, _playerStartType);
            
            OnTeamsAssigned?.Invoke();
        }
        
        StartCoroutine(StartGameBeginCountdown());
        StartCoroutine(GameTimer());
    }

    public Vector3 GetMaxSpacePoint()
    {
        return maxSpacePoint;
    }

    public Vector3 GetMaxSpacePointTakenForce()
    {
        return maxSpacePoint - new Vector3(forceDistance, forceDistance, forceDistance);
    }

    public float GetForceDistance()
    {
        return forceDistance;
    }

    public Vector3 GetMinSpacePoint()
    {
        return minSpacePoint;
    }

    public Vector3 GetMinSpacePointTakenForce()
    {
        return minSpacePoint + new Vector3(forceDistance, forceDistance, forceDistance);
    }

    public void ResetQuafflePosition()
    {
        quaffle.transform.position = quaffleResetPosition;
        quaffle.GetComponent<Quaffle>().ResetStatus();
    }

    public Vector3 GetQuaffleResetPosition() { return quaffleResetPosition; }

    private IEnumerator StartGameBeginCountdown()
    {
        _canPause = false;
        int countdown = gameStartCountdown;
        yield return new WaitForSeconds(1/Time.timeScale);
        while (countdown > 0)
        {
            GameUI.Instance.ShowZoomingMessage(true, countdown.ToString(), 0.75f/Time.timeScale);
            yield return new WaitForSeconds(1/Time.timeScale);
            countdown -= 1;
        }
        GameUI.Instance.ShowZoomingMessage(true, "START!", 0.75f/Time.timeScale);
        yield return new WaitForSeconds(1/Time.timeScale);
        GameUI.Instance.ShowZoomingMessage(false, "", 0);
        AudioManager.Instance.PlayWhistle();
        GameStarted = true;
        _canPause = true;
    }
    
    private IEnumerator GameTimer()
    {
        while (true)
        {
            while (GameStarted)
            {
                if(GameUI.Instance.IsPaused)
                    continue;
                
                if (timerSeconds <= 0)
                {
                    GameStarted = false;
                    AudioManager.Instance.PlayWhistle();
                    Debug.Log("Game Over");
                    GameOver(GameOverType.TIME_UP);
                    yield break;
                }
                if(timerSeconds == ((gameTimeMinutes*60)/2) && !_halftimeDone)
                {
                    _halftimeDone = true;
                    _canPause = false;
                    AudioManager.Instance.PlayWhistle();
                    yield return StartCoroutine(HalfTime());
                    continue;
                }
                timerSeconds -= 1;
                OnTimerUpdate?.Invoke(TimeSpan.FromSeconds(timerSeconds));
                yield return new WaitForSeconds(1/Time.timeScale);
            }

            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator HalfTime()
    {
        GameStarted = false;
        yield return new WaitForSeconds(1/Time.timeScale);
        ResetQuafflePosition();
        GameUI.Instance.ShowZoomingMessage(true, "HALF TIME", 0.75f/Time.timeScale);
        yield return new WaitForSeconds(1.5f/Time.timeScale);
        GameUI.Instance.ShowZoomingMessage(true, "SIDE CHANGE", 0.75f/Time.timeScale);
        yield return new WaitForSeconds(1);
        TeamManager.SwapTargets();
        SidesManager.Instance.SwapSides();
        SidesManager.Instance.ResetPositions();
        yield return StartCoroutine(StartGameBeginCountdown());
    }

    public void QuaffleScored(Team team)
    {
        OnQuaffleScored?.Invoke(team);
        ResetQuafflePosition();
        audienceManager.Celerbrate();
        GameUI.Instance.TeamScored(team);
        AudioManager.Instance.PlayCheerOnGoal();
        AudioManager.Instance.PlayGoalSFX();

        var rand = Random.Range(0, 10);
        if(rand%2 == 0)
            return;
        if(team == PlayerTeam)
            AudioManager.Instance.PlayMyGoalVO();
        else
        {
            AudioManager.Instance.PlayTheirGoalVO();
        }
    }

    public void GiveQuaffleToChaser(Team team)
    {
        Debug.Log($"GM:: Giving Quaffle to {team}");
        var chasers = TeamManager.GetChasersOfTeam(team);
        var targets = TeamManager.GetTargetsOfTeam(team);
        var minDist = Mathf.Infinity;
        var closestChaser = chasers[0];
        foreach (var chaser in chasers)
        {
            var dist = Vector3.Distance(targets[0].position, chaser.position);
            if (dist < minDist)
            {
                minDist = dist;
                closestChaser = chaser;
            }
        }
        var randChaser = closestChaser;
        randChaser.GetComponent<Role>().cachedQuaffle = quaffle;
        randChaser.GetComponent<Role>().TakeQuaffle();
        Debug.Log($"GM:: Quaffle given to {randChaser.name}");
    }

    public void GoldenSnitchScored(Team team)
    {
        OnGoldenSnitchScored?.Invoke(team);
        audienceManager.Celerbrate();
        GameOver(GameOverType.SNITCH_CAUGHT);
    }

    private void GameOver(GameOverType type)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _canPause = false;
        GameStarted = false;
        AudioManager.Instance.PlayWhistle();
        StartCoroutine(DelayedGameOver(type));
    }

    IEnumerator DelayedGameOver(GameOverType type)
    {
        yield return new WaitForSeconds(1);
        OnGameOver?.Invoke(type);
    }
}

