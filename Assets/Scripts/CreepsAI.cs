﻿using UnityEngine;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class CreepsAI : MonoBehaviour 
{
	[SerializeField]
	private Animator animator;
	[SerializeField]
	private NavMeshAgent navMeshAgent;
	[SerializeField]
	private float minDistToTarget;
	[SerializeField]
	private float timeUntilDespawn = 3f;

	public float ascendingModifier = 4; 
	public Vector3 targetPosition {get; set;}
	public int playerId {get; set;}
	public ParticleSystem HolyDespawn; 
	public bool isDead {get; private set;}
	public float speed {
		get { return navMeshAgent.speed; } 
		set {
			navMeshAgent.speed = value;
			animator.speed = value;
		}
	}

	private bool reachedTarget = false;

	//Since we are pooling these objects we have to reset all values here
	void OnEnable() 
	{
		StopAllCoroutines(); 
		isDead = false;
		this.transform.localScale = Vector3.one;
		this.reachedTarget = false; 
		this.navMeshAgent.enabled = true;
		this.navMeshAgent.avoidancePriority = Random.RandomRange( 0, 99 );
		navMeshAgent.ResetPath();
		if (!navMeshAgent.SetDestination( targetPosition )) {
			navMeshAgent.Stop();
			SimplePool.Despawn( this.gameObject );
		}
		this.HolyDespawn.Stop(); 
		this.HolyDespawn.Clear(); 
		this.HolyDespawn.time = 0; 
	}

	void OnDisable(){
		StopAllCoroutines(); 
	}

	void OnDestroy(){
		StopAllCoroutines(); 
	}
	void Update() 
	{

		if(!isDead && Vector3.Distance(this.transform.position, targetPosition) <= minDistToTarget) { 
			this.isDead = true; 
			Game.Instance.NotifyPlayerScrored(playerId);
			StopAllCoroutines(); 
			StartCoroutine(Co_Despawn());
			return;

		}

		if ( this.reachedTarget ) {
			var pos = this.transform.position; 
			pos.y += this.ascendingModifier*Time.deltaTime ; 
			this.transform.position = pos;
			return;
		}

		if(this.navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid && !this.navMeshAgent.pathPending) {
			navMeshAgent.ResetPath();
			if (!navMeshAgent.SetDestination( targetPosition )) {
				navMeshAgent.Stop();
				SimplePool.Despawn( this.gameObject );
			}
		}


	}

	public void Kill()
	{
		StartCoroutine(Co_Kill());
	}

	private IEnumerator Co_Despawn(){
		this.navMeshAgent.Stop(); 
		this.HolyDespawn.Clear(); 
		this.HolyDespawn.time = 0; 
		this.HolyDespawn.Play(); 
		this.reachedTarget = true;
		this.navMeshAgent.enabled = false; 
		yield return new WaitForSeconds( 2) ; 
		SimplePool.Despawn(this.gameObject);
	}
	private IEnumerator Co_Kill()
	{
		isDead = true;
		animator.Play("MinionStand");
		navMeshAgent.Stop();
		Vector3 scale = this.transform.localScale;
		scale.y = 0.1f;
		this.transform.localScale = scale;
		yield return new WaitForSeconds(timeUntilDespawn);
		SimplePool.Despawn(this.gameObject);
	}
}
