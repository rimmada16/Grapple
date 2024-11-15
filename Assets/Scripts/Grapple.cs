using System.Collections;
using UnityEngine;

/// <summary>
/// Handles the grapple gun mechanics
/// </summary>
public class Grapple : MonoBehaviour
{
    private bool _inGrapple;
    public bool InGrapple => _inGrapple;

    [Header("Components")]
    [SerializeField] private new Transform camera;
    [SerializeField] private Transform grappleGunEndpoint;
    [SerializeField] private LineRenderer lineRenderer;
    
    [Header("What can be interacted with")]
    [SerializeField] private LayerMask canGrapple;
    [SerializeField] private LayerMask canPull;
    
    [Header("Grapple Settings")]
    [SerializeField][Range(10f, 1000f)] private float grappleMaxDistance = 100f;
    [SerializeField][Range(2f, 20f)] private float grappleSpeed = 10f;
    [SerializeField][Range(1f, 3f)] private float grappleEndDistance = 1.5f;
    [SerializeField][Range(3f, 10f)] private float grapplePullUpwardForce = 5f;
    [SerializeField] private bool grappleCanIncreaseMomentum = true;
    
    [Header("Keybinds")]
    [SerializeField] private KeyCode grappleKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode grapplePullKey = KeyCode.Mouse1;
    
    private Vector3 _currentGrapplePoint;
    private RaycastHit _hit;
    private bool _inCoroutine;
    private bool _inPull;
    private CharacterController _characterController;

    /// <summary>
    /// Get the character controller component
    /// </summary>
    private void Start()
    {
        _characterController = GetComponent<CharacterController>();
        lineRenderer.positionCount = 0;
    }

    /// <summary>
    /// Checks the player input for grapple and grapple pull
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(grappleKey))
        {
            GrappleMotion();
        }
        else if (Input.GetKeyUp(grappleKey))
        {
            EndGrappleMotion();
        }

        if (Input.GetKeyDown(grapplePullKey))
        {
            PullGrappleMotion();
        }
        else if (Input.GetKeyUp(grapplePullKey))
        {
            _inPull = false;
            lineRenderer.positionCount = 0;
        }
    }

    /// <summary>
    /// Calls the grapple rope drawing function if doing some form of grapple
    /// </summary>
    private void LateUpdate()
    {
        if (_inGrapple)
        {
            DrawGrappleRope();
        }

        if (_inPull)
        {
            DrawGrappleRope();
        }
    }
    
    #region Grapple
    
    /// <summary>
    /// Checks if the player can grapple and calls the coroutine to move the player to the grapple point
    /// </summary>
    private void GrappleMotion()
    {
        if (Physics.Raycast(camera.position, camera.transform.forward, out _hit, grappleMaxDistance, canGrapple))
        {
            Debug.Log("Grapple");
            
            _inGrapple = true; // Signifies that the player is in a grapple - Tells the PlayerController to prevent movement inputs
            lineRenderer.positionCount = 2;
            
            if (!_inCoroutine)
            {
                StartCoroutine(TranslatePlayerToGrapplePoint());
            }
        }
    }

    /// <summary>
    /// Stops the moving of the player to the grapple point
    /// </summary>
    private void EndGrappleMotion()
    {
        _inGrapple = false;
        lineRenderer.positionCount = 0;
        
        if (_inCoroutine)
        {
            StopCoroutine(TranslatePlayerToGrapplePoint());
            _inCoroutine = false;
        }
    }

    /// <summary>
    /// Moves the player to the grapple point
    /// </summary>
    /// <returns></returns>
    private IEnumerator TranslatePlayerToGrapplePoint()
    {
        _inCoroutine = true;
        var increasingGrappleSpeed = grappleSpeed;
        
        while (_inGrapple && Vector3.Distance(transform.position, _hit.point) > 0.1f)
        {
            // If the player is close enough to the grapple point, end the grapple
            if (Vector3.Distance(transform.position, _hit.point) <= grappleEndDistance)
            {
                _inGrapple = false;
                break;
            }

            if (grappleCanIncreaseMomentum)
            {
                increasingGrappleSpeed *= 1.01f;
            }
            
            var direction = (_hit.point - transform.position).normalized;
            var move = direction * (increasingGrappleSpeed * Time.deltaTime);
            _characterController.Move(move);
            yield return null;
        }

        _inGrapple = false;
        _inCoroutine = false;
        EndGrappleMotion();
        yield return null;
    }

    #endregion
    
    #region GrapplePull
    
    /// <summary>
    /// Pulls a grappled object towards the player
    /// </summary>
    private void PullGrappleMotion()
    {
        if (Physics.Raycast(camera.position, camera.transform.forward, out _hit, grappleMaxDistance, canPull))
        {
            _inPull = true;
            
            var hitObject = _hit.collider.gameObject;
            var hitRigidbody = hitObject.GetComponent<Rigidbody>();
            
            lineRenderer.positionCount = 2;

            if (hitRigidbody != null)
            {
                var directionToPlayer = (transform.position - hitObject.transform.position).normalized;
                
                // Apply forces
                hitRigidbody.AddForce(directionToPlayer * grappleSpeed, ForceMode.VelocityChange);
                
                var upwardForce = new Vector3(0, 1, 0) * grapplePullUpwardForce;  
                hitRigidbody.AddForce(upwardForce, ForceMode.VelocityChange);
            }

            StartCoroutine(DisableLineRendererAfterPullTimer());
        }
    }

    /// <summary>
    /// Disables the line renderer after a set amount of time after the grapple pull
    /// </summary>
    /// <returns></returns>
    private IEnumerator DisableLineRendererAfterPullTimer()
    {
        yield return new WaitForSeconds(.2f);
        
        lineRenderer.positionCount = 0;
        _inPull = false;
    }
    
    #endregion
    
    /// <summary>
    /// Draws the grapple rope
    /// </summary>
    private void DrawGrappleRope()
    {
        if (lineRenderer.positionCount == 2)
        {
            // Player position
            lineRenderer.SetPosition(0, grappleGunEndpoint.position); 
            // Grapple point
            lineRenderer.SetPosition(1, _hit.point); 
        }
    }
}
