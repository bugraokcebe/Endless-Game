using System;
using System.Collections;
using JetBrains.Annotations;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace TempleRun.Player
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float initialPlayerSpeed = 4.0f;
        //[SerializeField] private float maximumPlayerSpeed = 30.0f;
        //[SerializeField] private float playerSpeedIncreaseRate = 0.1f;
        [SerializeField] private float jumpHeight = 1.0f;
        [SerializeField] private float initialGravityValue = -9.81f;

        [SerializeField] private float scoreMultiplier = 10f;
        
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask turnLayer;
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private Animator animator;
        [SerializeField] private AnimationClip slidingAnimationClip;


        [SerializeField] private float playerSpeed;
        private float _gravity;
        private Vector3 _movementDirection = Vector3.forward;
        private Vector3 _playerVelocity;
        private Vector3 direction;

        private PlayerInput _playerInput;
        private InputAction _turnAction;
        private InputAction _jumpAction;
        private InputAction _slideAction;
        public float solSag = 0;

        private int desiredLane = 1;
        public float laneDistance = 2;

        private CharacterController _controller;
        //private CharacterController controller;

        private int _slidingAnimationId;
        
        private bool _sliding = false;
        private float _score = 0;
        public float PlayerScore = 0;
        public Text Puan ;

        [SerializeField] private UnityEvent<Vector3> turnEvent;
        [SerializeField] private UnityEvent<int> gameOverEvent;
        [SerializeField] private UnityEvent<int> scoreUpdateEvent;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _controller = GetComponent<CharacterController>();            
            
            _turnAction = _playerInput.actions["Turn"];
            _jumpAction = _playerInput.actions["Jump"];
            _slideAction = _playerInput.actions["Slide"];
        }

        private void Start()
        {
            playerSpeed = initialPlayerSpeed;
            _gravity = initialGravityValue;
            _slidingAnimationId = Animator.StringToHash("SlidingAnimation");
        }
        
        private void Update()
        {

            if (!IsGrounded(20f))
            {
                GameOver1();
                return;
            }

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                desiredLane++;
                if (desiredLane == 3)
                {
                    desiredLane = 2;
                    
                }
                lineTurn(desiredLane);
                SAGSOL();
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {

                desiredLane--;
                if (desiredLane == -1)
                {
                    desiredLane = 0;
                }
                lineTurn(desiredLane);
                SAGSOL();
            }
            

            

            // Score Functionality
            _score += scoreMultiplier * Time.deltaTime;
            scoreUpdateEvent.Invoke((int)_score);
            
            _controller.Move(transform.forward * (playerSpeed * Time.deltaTime));

            if (IsGrounded() && _playerVelocity.y < 0)
            {
                _playerVelocity.y = -0.2f;
            }

            _playerVelocity.y += _gravity * Time.deltaTime;
            _controller.Move(_playerVelocity * (playerSpeed * Time.deltaTime));
        }

        private void OnEnable()
        {
            _turnAction.performed += OnTurnActionPerformed;
            _jumpAction.performed += OnJumpActionPerformed;
            _slideAction.performed += OnSlideActionPerformed;
        }

        private void OnDisable()
        {
            _turnAction.performed -= OnTurnActionPerformed;
            _jumpAction.performed -= OnJumpActionPerformed;
            _slideAction.performed -= OnSlideActionPerformed;
        }

        private void OnTurnActionPerformed(InputAction.CallbackContext context)
        {
            //Debug.Log("Player Turning.");
            solSag += context.ReadValue<float>();
            var turnPosition = CheckTurn(context.ReadValue<float>());
            Debug.Log(solSag);
            if (!turnPosition.HasValue)
            {
                GameOver1();
                return;
            }

            var targetDirection = Quaternion.AngleAxis(90 * context.ReadValue<float>(), Vector3.up) * _movementDirection;
            turnEvent.Invoke(targetDirection);

            Turn(context.ReadValue<float>(), turnPosition.Value);
        }

        private void Turn(float turnValue, Vector3 turnPosition)
        {
            var tempPlayerPosition = new Vector3(turnPosition.x, transform.position.y, turnPosition.z);

            _controller.enabled = false;
            transform.position = tempPlayerPosition;
            _controller.enabled = true;

            var targetRotation = transform.rotation * Quaternion.Euler(0, 90 * turnValue, 0);
            transform.rotation = targetRotation;
            _movementDirection = transform.forward.normalized;
        }
         private void lineTurn(int desiredLine1)
        {
            Vector3 targetPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
             //Vector3 targetPosition = transform.position.z * transform.forward + transform.position.y * transform.up;

            if(solSag % 4 == 0)
            {
                if (desiredLine1 == 0)
                {
                    targetPosition += Vector3.left * laneDistance;
                }
                else if (desiredLine1 == 2)
                {
                    targetPosition += Vector3.right * laneDistance;
                }
                else
                {
                    targetPosition += Vector3.zero * laneDistance;
                }
            }
            else if(solSag % 4 == -1 || solSag % 4 == 3) 
            {
                if (desiredLine1 == 0)
                {
                    targetPosition += Vector3.back * laneDistance;
                }
                else if (desiredLine1 == 2)
                {
                    targetPosition += Vector3.forward * laneDistance;
                }
                else
                {
                    targetPosition += Vector3.zero * laneDistance;
                }
            }
            else if (solSag % 4 == 1 || solSag % 4 == -3)
            {
                if (desiredLine1 == 0)
                {
                    targetPosition += Vector3.forward * laneDistance;
                }
                else if (desiredLine1 == 2)
                {
                    targetPosition += Vector3.back * laneDistance;
                }
                else
                {
                    targetPosition += Vector3.zero * laneDistance;
                }
            }
            else if(solSag %4 == -2 || solSag %4 == 2)
            {
                if (desiredLine1 == 0)
                {
                    targetPosition += Vector3.right * laneDistance;
                }
                else if (desiredLine1 == 2)
                {
                    targetPosition += Vector3.left * laneDistance;
                }
                else
                {
                    targetPosition += Vector3.zero * laneDistance;
                }
            }

      

            //_controller.Move(direction * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPosition, 200 * Time.deltaTime);
            _controller.center = _controller.center;
        }
        private Vector3? CheckTurn(float turnValue)
        {
            var hitColliders = Physics.OverlapSphere(transform.position, 0.1f, turnLayer);

            if (hitColliders.Length > 0)
            {
                var tile = hitColliders[0].transform.parent.GetComponent<Tile>();
                var type = tile.type;
                if ((type == TileType.LEFT && turnValue == -1) ||
                    (type == TileType.RIGHT && turnValue == 1) ||
                    (type == TileType.SIDEWAYS))
                {
                    return tile.pivot.position;
                }
            }
            return null;
        }

        private void OnJumpActionPerformed(InputAction.CallbackContext context)
        {
            if (IsGrounded())
            {
                //Debug.Log("Jumping");
                _playerVelocity.y += Mathf.Sqrt(jumpHeight * _gravity * -3f);
                _controller.Move(_playerVelocity * Time.deltaTime);
            }
            else
            {
                //Debug.Log("Jump button pressed but not grounded.");
            }
        }

        private void OnSlideActionPerformed(InputAction.CallbackContext context)
        {
            if (!_sliding && IsGrounded())
            {
                StartCoroutine(Slide());
            }
        }

        private IEnumerator Slide()
        {
            _sliding = true;
            
            // Shrink the collider
            var originalControllerCenter = _controller.center;
            var newControllerCenter = originalControllerCenter;
            _controller.height /= 2;
            newControllerCenter.y -= _controller.height / 2;
            _controller.center = newControllerCenter;

            // Play sliding animation.
            animator.Play(_slidingAnimationId);
            yield return new WaitForSeconds(slidingAnimationClip.length);
            
            // Change controller center and height back to normal.
            _controller.height *= 2;
            _controller.center = originalControllerCenter;
            _sliding = false;
        }

        private bool IsGrounded(float length = 0.5f)
        {
            var rayCastOriginFirst = transform.position;
            rayCastOriginFirst.y -= _controller.height / 2f;
            // rayCastOriginFirst.y += 0.1f;

            var rayCastOriginSecond = rayCastOriginFirst;
            rayCastOriginFirst -= transform.forward * 0.2f;
            rayCastOriginSecond += transform.forward * 0.2f;

            var ray = new Ray(rayCastOriginFirst, Vector3.down);
            var ray2 = new Ray(rayCastOriginSecond, Vector3.down);

            Debug.DrawLine(rayCastOriginFirst, rayCastOriginFirst + Vector3.down * length, Color.green, .1f);
            Debug.DrawLine(rayCastOriginSecond, rayCastOriginSecond + Vector3.down * length, Color.blue, .1f);


            if (Physics.Raycast(ray, length, groundLayer) || Physics.Raycast(ray2, length, groundLayer))
            {
                // Debug.Log("Player is grounded.");
                return true;
            }

            // Debug.Log("Player is not grounded.");
            return false;
        }

        private void GameOver()
        {
            PlayerManager.gameOver = true;

        }
        private void GameOver1()
        {
            PlayerManager.gameOver = true;
        }
        private void SAGSOL()
        {
            Debug.Log("saðsol");
        }


        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.transform.tag == "Obstacle")
            {
                PlayerManager.gameOver = true;
            }
        }
    }
}