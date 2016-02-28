using UnityEngine;
using System;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Net;
using ApiAiSDK;
using ApiAiSDK.Model;
using ApiAiSDK.Unity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class PlayerControl : MonoBehaviour
{
	private ApiAiUnity apiAiUnity;
	private AudioSource aud;
	bool speechInput = false;

	private readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
	{ 
		NullValueHandling = NullValueHandling.Ignore,
	};

	public void StartListening()
	{
		Debug.Log("StartListening");
		aud = GetComponent<AudioSource>();
		apiAiUnity.StartListening(aud);
	}

	public void StopListening()
	{
		try
		{
			Debug.Log("StopListening");
			apiAiUnity.StopListening();
		} catch (Exception ex)
		{
			Debug.LogException(ex);
		}
	}

	void HandleOnResult(object sender, AIResponseEventArgs e)
	{
		var aiResponse = e.Response;
		if (aiResponse != null) {
			var outText = JsonConvert.SerializeObject (aiResponse, jsonSettings);
			Debug.Log (outText);

			string command = "jump"; //define additional "grammars" here if necessary

			//return true if JSON request string contains specified command
			if (outText.Contains(command)) {
				speechInput = true;
			}

			//NOTE:
			//Given more time, the more optimal solution would be to parse the JSON request string
			//and access the "resolvedQuery" field directly

		} else {
			Debug.LogError("Response is null");
		}
	}

	void HandleOnError(object sender, AIErrorEventArgs e)
	{
		Debug.LogException(e.Exception);
	}

	// Initialization
	IEnumerator Start()
	{
		// check access to the Microphone
		yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
		if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
		{
			throw new NotSupportedException("Microphone using not authorized");
		}

		ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) =>
		{
			return true;
		};
		const string SUBSCRIPTION_KEY = "16f5bce1-7673-493e-9a85-ecc4921ad29d";
		const string ACCESS_TOKEN = "f55f28edf93e4118b73b78e4271b5d39";

		var config = new AIConfiguration (SUBSCRIPTION_KEY, ACCESS_TOKEN, SupportedLanguage.English);

		apiAiUnity = new ApiAiUnity ();
		apiAiUnity.Initialize (config);

		apiAiUnity.OnResult += HandleOnResult;
		apiAiUnity.OnError += HandleOnError;
	}

	/// <summary>
	/// player control code
	/// </summary>

	[HideInInspector]
	public bool facingRight = true;			// For determining which way the player is currently facing.
	[HideInInspector]
	public bool jump = false;				// Condition for whether the player should jump.


	public float moveForce = 365f;			// Amount of force added to move the player left and right.
	public float maxSpeed = 5f;				// The fastest the player can travel in the x axis.
	public AudioClip[] jumpClips;			// Array of clips for when the player jumps.
	public float jumpForce = 1000f;			// Amount of force added when the player jumps.
	public AudioClip[] taunts;				// Array of clips for when the player taunts.
	public float tauntProbability = 50f;	// Chance of a taunt happening.
	public float tauntDelay = 1f;			// Delay for when the taunt should happen.


	private int tauntIndex;					// The index of the taunts array indicating the most recent taunt.
	private Transform groundCheck;			// A position marking where to check if the player is grounded.
	private bool grounded = false;			// Whether or not the player is grounded.
	private Animator anim;					// Reference to the player's animator component.


	void Awake()
	{
		// Setting up references.
		groundCheck = transform.Find("groundCheck");
		anim = GetComponent<Animator>();
	}


	void Update()
	{
		if (apiAiUnity != null)
		{
			apiAiUnity.Update();
		}
		// The player is grounded if a linecast to the groundcheck position hits anything on the ground layer.
		grounded = Physics2D.Linecast(transform.position, groundCheck.position, 1 << LayerMask.NameToLayer("Ground"));  

		// If the jump button is pressed and the player is grounded then the player should jump.
		//if(Input.GetButtonDown("Jump") && grounded)
		if(speechInput == true && grounded)
			jump = true;
		
	}


	void FixedUpdate ()
	{
		bool Listen = Input.GetButton ("Listen");
		bool Stoplisten = Input.GetButton ("Stoplisten");

		// Cache the horizontal input.
		float h = Input.GetAxis("Horizontal");

		// The Speed animator parameter is set to the absolute value of the horizontal input.
		anim.SetFloat("Speed", Mathf.Abs(h));

		// If the player is changing direction (h has a different sign to velocity.x) or hasn't reached maxSpeed yet...
		if(h * GetComponent<Rigidbody2D>().velocity.x < maxSpeed)
			// ... add a force to the player.
			GetComponent<Rigidbody2D>().AddForce(Vector2.right * h * moveForce);

		// If the player's horizontal velocity is greater than the maxSpeed...
		if(Mathf.Abs(GetComponent<Rigidbody2D>().velocity.x) > maxSpeed)
			// ... set the player's velocity to the maxSpeed in the x axis.
			GetComponent<Rigidbody2D>().velocity = new Vector2(Mathf.Sign(GetComponent<Rigidbody2D>().velocity.x) * maxSpeed, GetComponent<Rigidbody2D>().velocity.y);

		// If the input is moving the player right and the player is facing left...
		if(h > 0 && !facingRight)
			// ... flip the player.
			Flip();
		// Otherwise if the input is moving the player left and the player is facing right...
		else if(h < 0 && facingRight)
			// ... flip the player.
			Flip();

		// If the player should jump...
		if(speechInput == true)
		{
			// Set the Jump animator trigger parameter.
			anim.SetTrigger("Jump");

			// Play a random jump audio clip.
			int i = UnityEngine.Random.Range(0, jumpClips.Length);
			AudioSource.PlayClipAtPoint(jumpClips[i], transform.position);

			// Add a vertical force to the player.
			GetComponent<Rigidbody2D>().AddForce(new Vector2(0f, jumpForce));

			// Make sure the player can't jump again until the jump conditions from Update are satisfied.
			jump = false;
			speechInput = false;
		}

		if (Listen) {
			StartListening ();
		}

		if (Stoplisten) {
			StopListening ();
		}
	}
	
	
	void Flip ()
	{
		// Switch the way the player is labelled as facing.
		facingRight = !facingRight;

		// Multiply the player's x local scale by -1.
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}


	public IEnumerator Taunt()
	{
		// Check the random chance of taunting.
		float tauntChance = UnityEngine.Random.Range(0f, 100f);
		if(tauntChance > tauntProbability)
		{
			// Wait for tauntDelay number of seconds.
			yield return new WaitForSeconds(tauntDelay);

			// If there is no clip currently playing.
			if(!GetComponent<AudioSource>().isPlaying)
			{
				// Choose a random, but different taunt.
				tauntIndex = TauntRandom();

				// Play the new taunt.
				GetComponent<AudioSource>().clip = taunts[tauntIndex];
				GetComponent<AudioSource>().Play();
			}
		}
	}


	int TauntRandom()
	{
		// Choose a random index of the taunts array.
		int i = UnityEngine.Random.Range(0, taunts.Length);

		// If it's the same as the previous taunt...
		if(i == tauntIndex)
			// ... try another random taunt.
			return TauntRandom();
		else
			// Otherwise return this index.
			return i;
	}
}