﻿using UnityEngine;
using System.Collections;
using System;

// This is going to control the game (like spawns, win conditions, joins, leaves, etc.)

public class Game : SingletonMonoBehaviour<Game> {

	public class GameResults
	{
		public class TeamResults
		{
			public bool Winner;
			public int KilledMinions;
			public int KilledOwnMinions;
			public int SavedMinions;
		}

		public TeamResults[] Results;
	}

	public static GameResults Results;
	public float CountdownLength = 4.0f;
	public int TargetScore = 100;
	public AudioSource audioSource;

	int[] TeamScores = new int[2];

	public enum GameStateId
	{
		WaitingForPlayers,
		Countdown,
		Playing,
		Ending
	}

	GameStateId _GameState;
	public GameStateId GameState
	{
		get
		{
			return _GameState;
		}
		private set
		{
			if(_GameState != value) {
				_GameState = value;
				EventGameStateChanged( value );
			}
		}
	}

	public int NumberOfPlayers
	{
		get; private set;
	}

	Player[] Players = new Player[4];
	float RemainingCountdown = 0.0f;

	public event Action<GameStateId> EventGameStateChanged = (newState) => { };
	public event Action<int, bool> EventPlayerCanJoinChanged = (index, canJoin) => { };
	public event Action<Player> EventPlayerJoined = (player) => { };
	public event Action<Player, bool> EventPlayerReadyChanged = (player, isReady) => { };
	public event Action<int> EventTeamScored = (teamId) => { };
	public event Action<int> EventPlayerScored = (playerId) => { };
	public event Action<int, int> EventPlayerKilledMinion = (playerId, ownerId) => { };

	public int GetTeamScore(int teamIndex) {
		return this.TeamScores[teamIndex];
	}

	public void NotifyPlayerScrored(int playerId) {
		int teamIndex = Lobby.GameConfig.PlayerTeamNumbers[playerId];
		this.TeamScores[teamIndex]++;
		this.Players[playerId].Score++;
		EventTeamScored( teamIndex );
		EventPlayerScored( playerId );
		if(this.TeamScores[teamIndex] >= this.TargetScore) {
			SaveGameResults();
			this.GameState = GameStateId.Ending;
			Invoke( "GoToMainmenu", 3.0f );
		}
	}

	void SaveGameResults () {
		Results = new GameResults();
		Results.Results = new GameResults.TeamResults[2];
		int maxScore = 0;
		for(int i = 0; i < 2; i++) {
			maxScore = Mathf.Max( maxScore, TeamScores[i] );
		}
		for(int i = 0; i < 2; i++) {
			var teamResult = new GameResults.TeamResults();
			for(int j = 0; j < 4; j++) {
				if(this.Players[j] != null) {
					var team = Lobby.GameConfig.PlayerTeamNumbers[j];
					if(team == i) {
						teamResult.KilledMinions += this.Players[j].EnemyMinionsKilled;
						teamResult.KilledOwnMinions += this.Players[j].OwnMinionsKilled;
					}
				}
			}
			teamResult.SavedMinions = TeamScores[i];
			teamResult.Winner = teamResult.SavedMinions == maxScore;
			Results.Results[i] = teamResult;
		}
	}

	void GoToMainmenu () {
		Application.LoadLevel( "ResultScreen" );
	}

	public void NotifyPlayerKill(int playerId, int ownerId) {
		int playerTeam = Lobby.GameConfig.PlayerTeamNumbers[playerId];
		int minionTeam = Lobby.GameConfig.PlayerTeamNumbers[ownerId];

		if (playerTeam == minionTeam) {
			this.Players[playerId].OwnMinionsKilled++;
		} else {
			this.Players[playerId].EnemyMinionsKilled++;
		}

		EventPlayerKilledMinion( playerId, ownerId );
		audioSource.Play();
	}

	void Start () {
		_GameState = GameStateId.WaitingForPlayers;
		for (int i = 0; i < 4; i++) {
			Map.Instance.CreepsSpawners[i].gameObject.SetActive( false );
		}
		if (Lobby.GameConfig == null)
			return;
		for (int i = 0; i < 4; i++) {
			if(Lobby.GameConfig.PlayerTeamNumbers[i] != -1) {
				AddPlayer( i );
			}
		}
		this.RemainingCountdown = this.CountdownLength;
		this.GameState = GameStateId.Countdown;

	}

	void Update () {
		switch (this.GameState) {
			case GameStateId.Countdown:
				this.RemainingCountdown -= Time.deltaTime;
				if(this.RemainingCountdown <= 0.0f) {
					this.GameState = GameStateId.Playing;
				}
				break;
		}
	}

	[ContextMenu("AddPlayer")]
	public void AddPlayerAtFirstAvailableSpot () {
		for(int i = 0; i < 4; i++) {
			if(this.Players[i] == null) {
				AddPlayer( i );
				return;
			}
		}
	}

	public Player GetPlayer (int index) {
		return Players[index];
	}

	public void AddPlayer (int index) {
		var playerSpawner = Map.Instance.PlayerSpawners[index];
		if (playerSpawner == null) {
			Debug.LogError( "Could not find matching spawner for player index " + index );
			return;
		}
		NumberOfPlayers++;
		var newPlayer = PlayerFactory.Instance.CreatePlayerInstance( index, playerSpawner.Position, playerSpawner.Rotation );
		this.Players[index] = newPlayer;
		Map.Instance.CreepsSpawners[index].gameObject.SetActive( true );
		EventPlayerJoined( newPlayer );
	}
	
	public void PlayerNotReady (int index) {
		if (this.GameState != GameStateId.WaitingForPlayers)
			return;
		this.Players[index].PlayerReady = false;
		EventPlayerReadyChanged( this.Players[index], false );
	}

	public void PlayerReady(int index) {
		if (this.GameState != GameStateId.WaitingForPlayers)
			return;
		this.Players[index].PlayerReady = true;
		EventPlayerReadyChanged( this.Players[index], true );
		CheckIfAllPlayersAreReady();
	}

	bool[] PlayerCanJoin = new bool[4];
	public bool CanPlayerJoin(int index) {
		return PlayerCanJoin[index];
	}

	public void NotifyPlayerCanJoin(int index) {
		PlayerCanJoin[index] = true;
		EventPlayerCanJoinChanged( index, true );
	}

	public void NotifyPlayerCanNotJoin (int index) {
		PlayerCanJoin[index] = false;
		EventPlayerCanJoinChanged( index, false );
	}

	bool AllPlayersReady
	{
		get
		{
			if (this.NumberOfPlayers == 0)
				return false;
			for(int i = 0; i < 4; i++) {
				var player = this.Players[i];
				if (player != null && !player.PlayerReady)
					return false;
			}
			return true;
		}
	}

	void CheckIfAllPlayersAreReady () {
		if (AllPlayersReady) {
			this.RemainingCountdown = this.CountdownLength;
			this.GameState = GameStateId.Countdown;
		}
	}
}
