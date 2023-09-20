using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RockBase : MonoBehaviour, IHitObjectHandler
{
    //돌 하위 오브젝트들
    [SerializeField]
    protected Transform rockObject;
    [SerializeField]
    protected Transform checkPoint;
    [SerializeField]
    public RockStatus rockStatus;
    [SerializeField]
    protected List<Mesh> forms;

    //사용자 입력(Y축 제외)
    protected Vector2 playerInput;

    //기본 컴포넌트
    protected Camera mainCamera;
    protected Rigidbody rockRigidbody;
    protected MeshRenderer rockRenderer;
    protected MeshFilter rockMesh;
    protected MeshCollider rockCollider;
    
    protected float currHp;
    protected float obstacleMultiple = 1;

    protected bool isGround = false;
    protected bool isSlope = false;
    protected bool isFall = false;
    //{ 0920 홍한범
    // 디버프 체크
    public bool isDebuffed = false;
    // 점프포스 감소
    public float debuffJumpForce;
    //} 0920 홍한범

    RaycastHit slopeHit;
    Coroutine fallCheckCoroutine = null;

    protected const float DAMAGE_LIMIT_MIN = 50f;
    protected const float SLOPE_LIMIT_MAX = 60f;
    protected const float COLLISION_ALLOW_ANGLE = 45f;
    protected const float DEFAULT_JUMP_FORCE = 5f;
    protected const float DEFAULT_GROUND_DRAG = .1f;
    protected const float DEFAULT_AIR_MULTIPLE = .3f;
    protected const float DEFAULT_SLOPE_MULTIPLE = 1.5f;
    protected const float DEFAULT_OBSTACLE_MULTIPLE = 1f;

    private void OnDrawGizmos()
    {
        if (rockObject != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(rockObject.position - Vector3.up * rockObject.gameObject.GetHeight(.5f), .05f);
        }
    } //사랑해요 성철이형 -재현-
    //GOOD


    //{ 0920 홍한범
    // 속도에 대한 저항값, 질량에 대한 저항값을 적용할 수 있음
    // 감소수치를 넣고 SetObstacleMultiple를 부르고 ResetDebuff를 부르면 풀림
    // 모든값은 퍼센트로 줄임 Ex) 2배 2f, 0.5배 0.5f;
    public void SetObstacleMultiple(float velocityValue, float massValue, float jumpValue)
    {
        if (isDebuffed == false)
        {
            obstacleMultiple *= velocityValue;
            rockRigidbody.mass = rockStatus.Weight * massValue;
            debuffJumpForce = jumpValue;
            isDebuffed = true;
        }
    }

    public void ResetDebuff()
    {
        if (isDebuffed == true)
        {
            obstacleMultiple = 1f;
            rockRigidbody.mass = rockStatus.Weight;
            debuffJumpForce = 1f;
            isDebuffed = false;
        }
    }
    //} 0920 홍한범

    public virtual void Init()
    {
        //추후 시네머신 카메라로 바꿀것
        mainCamera = Camera.main;

        rockObject = transform.Find("RockObject");
        checkPoint = transform.Find("CheckPoint");
        checkPoint.position = rockObject.position;

        rockRigidbody = rockObject.GetComponent<Rigidbody>();
        rockMesh = rockObject.GetComponent<MeshFilter>();
        rockCollider = rockObject.GetComponent<MeshCollider>();
        rockRenderer = rockObject.GetComponent<MeshRenderer>();

        rockStatus = new RockStatus(rockStatus);

        rockRigidbody.mass = rockStatus.Weight;
        currHp = rockStatus.Health;
        //{ 0920 홍한범
        debuffJumpForce = 1f;
        //} 0920 홍한범
    }

    //혹시 모를 오버로딩
    protected virtual void Move()
    {
        playerInput = GetInput();
        Move(playerInput);
    }
    protected virtual void Jump()
    {
        Jump(DEFAULT_JUMP_FORCE);
    }


    //이동
    protected virtual void Move(Vector2 input)
    {
        float accel = rockStatus.Acceleration * obstacleMultiple;
        float maxSpeed = rockStatus.Speed * obstacleMultiple;

        //카메라를 기준으로 좌표 변경
        Vector3 inputDirection = (Camera.main.transform.forward * input.y + Camera.main.transform.right * input.x).normalized;
        //y축 고려 x
        Vector3 groundValocity = new Vector3(rockRigidbody.velocity.x, 0f, rockRigidbody.velocity.z);

        //이동방향으로 
        inputDirection *= accel * Time.deltaTime;
        groundValocity += inputDirection;

        //속도 변화
        Vector3 newVelocity = (new Vector3(inputDirection.x, 0, inputDirection.z));
        //점프
        if (!isGround)
        {
            rockRigidbody.velocity += newVelocity * DEFAULT_AIR_MULTIPLE;
        }
        //경사
        else if (CheckSlope())
        {
            rockRigidbody.velocity += GetSlopeDirection(newVelocity)* DEFAULT_SLOPE_MULTIPLE;
        }
        //땅
        else
        {
            rockRigidbody.velocity += newVelocity;
        }


        //속도 제한
        //땅
        if (groundValocity.magnitude > maxSpeed)
        {
            rockRigidbody.velocity = groundValocity.normalized * maxSpeed + Vector3.up * rockRigidbody.velocity.y;
        }
        //경사
        else if(isSlope && rockRigidbody.velocity.magnitude>maxSpeed)
        {
            rockRigidbody.velocity = rockRigidbody.velocity.normalized * maxSpeed;
        }

    }

    //점프
    protected virtual void Jump(float power)
    {
        rockRigidbody.velocity += Vector3.up * (power*debuffJumpForce);
    }


    //레거시 
    [Obsolete]
    protected virtual bool IsGround()
    {
        float rockHeightHalf = rockObject.gameObject.GetHeight(.5f);
        Collider[] colliders = Physics.OverlapSphere(rockObject.position - Vector3.up * rockHeightHalf, .05f, Global_PSC.FindLayerToName("Terrains"));

        if (colliders.Length > 0)
        {
            return true;
        }
        return false;
    }


    //오버랩을 사용하는 checkGround
    protected virtual bool CheckGround()
    {

        if (isSlope)
        {
            return CheckGroundRay();
        }
        else
        {
            return CheckGroundOverlap();
        }

    }
    //오버랩을 사용하는 checkGround
    protected virtual bool CheckGroundOverlap()
    {
        isGround = false;
        float rockHeightHalf = rockObject.gameObject.GetHeight(.5f);

        Collider[] colliders = Physics.OverlapSphere(rockObject.position - Vector3.up * rockHeightHalf, .05f, Global_PSC.FindLayerToName("Terrains"));
        if (colliders.Length > 0)
        {
            isGround = true;
        }

        return isGround;
    }

    //레이를 사용하는 checkGround
    //경사 구조때문에 해당 메서드 사용 권장
    protected virtual bool CheckGroundRay()
    {
        isGround = false;
        float rockHeightHalf = rockObject.gameObject.GetHeight(.5f);

        if (Physics.Raycast(rockObject.position, Vector3.down, out slopeHit, rockHeightHalf + rockHeightHalf * .75f, Global_PSC.FindLayerToName("Terrains")))
        {
            isGround = true;
        }

        return isSlope;
    }

    //경사로 체크
    //레이 사용
    protected virtual bool CheckSlope()
    {
        isSlope = false;
        float rockHeightHalf = rockObject.gameObject.GetHeight(.5f);

        if (Physics.Raycast(rockObject.position, Vector3.down, out slopeHit, rockHeightHalf + rockHeightHalf * .75f, Global_PSC.FindLayerToName("Terrains")))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            if (angle != 0 && angle <= SLOPE_LIMIT_MAX)
            {
                isSlope = true;
            }
        }

        return isSlope;
    }

    //떨어짐 체크
    //레이 사용
    //바닥이 없는 상황에서 계산 시작
    protected virtual void CheckFall()
    {
        if (!Physics.Raycast(rockObject.position, Vector3.down, 1000, Global_PSC.FindLayerToName("Terrains")) && fallCheckCoroutine==null)
        {
            StartFallingCheck();
        }
    }

    
    //일정시간동안 검사하기 위해서 coroutine으로 검사 시작.
    protected virtual void StartFallingCheck()
    {
        if (fallCheckCoroutine != null)
        {
            StopCoroutine(fallCheckCoroutine);
            fallCheckCoroutine = null;
        }

        fallCheckCoroutine = StartCoroutine(FallingCheckRoutine(2));
        
    }

    //일정 시간동안 바닥이 없을경우
    // checkpoint로 back시킴
    //이때 떨어지는화면->손->실제 플레이 순으로 진행되기 때문에 coroutine으로 구현
    IEnumerator FallingCheckRoutine(float needTime)
    {
        float time = 0;
        while (time < needTime)
        {
            if (isGround)
            {
                isFall = false;
                fallCheckCoroutine = null;
                yield break;
            }
            yield return Time.deltaTime;
            time += Time.deltaTime;
        }

        isFall = true;
        StartCoroutine(ComebackCheckPointRoutine());
    }


    IEnumerator ComebackCheckPointRoutine()
    {
        //카메라 정지
        //떨어지면서 메아리 추가
        Fall();
        yield return new WaitForSeconds(1f);
        //손 생성 애니메이션?
        yield return new WaitForSeconds(1f);
        //되돌리기
        BackCheckPoint();
    }

    //경사에 있을 경우 힘의 방향을 해당 경사에 맞게 회전시킴
    protected Vector3 GetSlopeDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal);
    }


    //공격
    //hitobject가 있을 경우 해당 오브젝트를 공격한다.
    //속도가 있어야하고
    //징행 방향과 비슷한 방향이어야 공격이라 판단하며
    //공격때 자신의 체력도 닳게한다.
    public virtual void Attack(Collision collision)
    {
        IHitObjectHandler hitObject = collision.gameObject.GetComponentInParent<IHitObjectHandler>();
        if (hitObject == null)
        {
            hitObject = collision.gameObject.GetComponent<IHitObjectHandler>();
            if (hitObject == null)
            {
                return;
            }
        }

        Debug.Log(hitObject);
        foreach (ContactPoint contact in collision.contacts)
        {
            Debug.Log(contact.thisCollider.transform.parent.gameObject + "/"+ gameObject);
            if (contact.thisCollider.transform.parent.gameObject == gameObject)
            {

                hitObject.Hit((int)GetDamageValue());
                Debug.Log(GetDamageValue());
                break;

                /* // 충돌 지점의 법선 벡터와 gameobject의 진행 방향을 계산합니다.
                 Vector3 collisionNormal = contact.normal;
                 Vector3 forwardDirection = rockRigidbody.velocity.normalized;

                 // 두 벡터의 각도를 계산합니다.
                 float angle = Vector3.Angle(collisionNormal, forwardDirection);

                 // 일정 각도 이내의 충돌을 확인합니다.
                 float maxCollisionAngle = COLLISION_ALLOW_ANGLE; // 예시: 45도 이내의 충돌을 확인
                 Debug.Log(angle + "/" + maxCollisionAngle);
 */
               /* if (angle <= maxCollisionAngle)
                {
                    hitObject.Hit((int)GetDamageValue());
                    break;
                }
                else
                {
                    continue;
                }*/
            }

        }
    }


    protected virtual float Attack()
    {
        return 0;
    }

    protected virtual void Fall()
    {
        CinemachineVirtualCameraBase camera = mainCamera.GetComponent<CinemachineBrain>().ActiveVirtualCamera as CinemachineVirtualCameraBase;
        camera.Follow = null;
    }
    protected virtual void BackCheckPoint()
    {
        CinemachineVirtualCameraBase camera = mainCamera.GetComponent<CinemachineBrain>().ActiveVirtualCamera as CinemachineVirtualCameraBase;
        camera.Follow = rockObject;
        fallCheckCoroutine = null;
        isFall = false;
        rockRigidbody.velocity = Vector3.zero;
        rockRigidbody.angularVelocity = Vector3.zero;
        rockObject.position = checkPoint.position + Vector3.up * 10f;

        Debug.Log(rockObject.position);
        Debug.Log(checkPoint.position);
    }

    //맞았을 경우 체력마다 다른 mesh를 보여준다.
    public void Hit(int damage)
    {
        HitReaction();
        currHp -= damage;
        if (currHp < 0)
        {
            Die();
        }
        else
        {
            ChangeForm(currHp);
        }
    
    }

    

    public void SetCheckPoint(Vector3 position)
    {
        checkPoint.position = position;
    }

    protected virtual void ChangeDrag()
    {

        if (isGround)
        {
            rockRigidbody.drag = DEFAULT_GROUND_DRAG;
        }
        else
        {
            rockRigidbody.drag = .01f/obstacleMultiple;
        }
    }

    protected virtual void ChangeForm(float hp)
    {
        if (forms==null || forms.Count<1)
        {
            return;
            
        }
        float changeRate = rockStatus.Health / forms.Count;
        int formIndex = (int)(currHp / changeRate);
        
    }

    public void HitReaction(){}

    protected virtual void Die(){}

    protected Vector2 GetInput()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        playerInput = new Vector2(horizontalInput, verticalInput);
        return playerInput;
    }

    public float NowSpeed()
    {
        return rockRigidbody.velocity.magnitude;

    }

    public bool IsMove()
    {
        return rockRigidbody.velocity.magnitude > 0;
    }
    public bool IsMove(float speed)
    {
        return rockRigidbody.velocity.magnitude >= speed;
    }

    //공격력 * 0.5 * 최대체력 대비 현재체력 +공격력 * 0.5 * 최대속도 대비 현재속도
    //예외 공격력 50이하 일 경우 50
    protected float GetDamageValue()
    {
        float maxDamage = rockStatus.Damage;
        float resultDamage = 0;

        float healthRate = currHp / rockStatus.Health;
        float speedRate = rockRigidbody.velocity.magnitude / rockStatus.Speed;

        resultDamage += maxDamage * .5f * healthRate;
        resultDamage += maxDamage * .5f * speedRate;

        if (resultDamage < DAMAGE_LIMIT_MIN)
        {
            resultDamage = DAMAGE_LIMIT_MIN;
        }

        return resultDamage;
    }

    protected RockState rockState;
    public enum RockState
    {
        MOVE,
        JUMP
    }

    protected void ChangeRockState()
    {
        if (isGround)
        {
            rockState = RockState.MOVE;
        }

        else if (!isGround)
        {
            rockState = RockState.JUMP;
        }
    }
}

