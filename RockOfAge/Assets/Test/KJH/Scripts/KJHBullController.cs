using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics;
using UnityEngine;
using System;

public class KJHBullController : MonoBehaviour
{
    public float detectionRange = default;
    public float chargeSpeed = 10.0f;
    public LayerMask Rock;

    public float health = 100f; // 체력 변수
    public float attackPower = default; // 공격력 변수
    public float chargeCool = 5.0f;
    public float walkSpeed = 3.0f;

    private Vector3 lastRockPosition; // 돌의 마지막 위치를 저장할 변수 추가
    private bool hasCharged = false; // 모루황소가 돌진했는지 여부를 나타내는 변수 추가
    private float lastChargeTime = 0f;
    private Rigidbody bullRigidbody;
    private bool isCharging = false;
    private Transform targetRock;
    private Animator animator;
    private int chargeCount = 0;
    private Vector3 initialBullPosition;
    private bool isReturning = false;

    private void Start() // Start 메서드를 선언합니다.
    {
        animator = GetComponent<Animator>(); // 애니메이터 컴포넌트를 가져옵니다.
        bullRigidbody = GetComponent<Rigidbody>(); // 리지드바디 컴포넌트를 가져옵니다.
        initialBullPosition = transform.position;
    }


    //private void Update()
    //{
    //    if (!isCharging)
    //    {
    //        lastChargeTime += Time.deltaTime;
    //        if (lastChargeTime >= chargeCool)
    //        {
    //            DetectRock();
    //            if (isCharging)
    //            {
    //                lastChargeTime = 0f;
    //            }
    //        }
    //    }
    //    else
    //    {
    //        ChargeTowardsRock();

    //        float distanceToLastRockPosition = Vector3.Distance(transform.position, lastRockPosition);

    //        // 돌진 중일 때 돌의 마지막 좌표에 도착했는지 확인합니다.
    //        if (distanceToLastRockPosition <= 10.0f)
    //        {
    //            Debug.LogFormat("????");
    //            ResetCharge();
    //        }
    //    }
    //    // 머리가 돌을 바라보게 합니다.
    //    if (targetRock != null)
    //    {
    //        if (isCharging)
    //        {
    //            // 돌진 중일 때는 돌진 방향을 바라봅니다.
    //            Vector3 chargeDirection = (lastRockPosition - transform.position).normalized;
    //            chargeDirection.y = 0;
    //            transform.rotation = Quaternion.LookRotation(chargeDirection);
    //        }
    //        else
    //        {
    //            // 돌진 중이 아닐 때는 돌을 바라봅니다.
    //            transform.LookAt(new Vector3(targetRock.transform.position.x, transform.position.y, targetRock.transform.position.z));
    //        }
    //    }
    //}
    private void Update()
    {
        animator.SetBool("isCharging", isCharging);
        animator.SetBool("isReturning", isReturning);

        if (!isCharging && !isReturning)
        {
            lastChargeTime += Time.deltaTime;
            if (lastChargeTime >= chargeCool)
            {
                DetectRock();
                if (isCharging)
                {
                    lastChargeTime = 0f;
                }
            }
        }
        else if (isCharging)
        {
            ChargeTowardsRock();

            float distanceToLastRockPosition = Vector3.Distance(transform.position, lastRockPosition);

            if (distanceToLastRockPosition <= 10.0f)
            {
                Debug.LogFormat("????");
                isCharging = false;
                isReturning = true;
            }
        }
        else if (isReturning)
        {
            ReturnToInitialPositionBackwards();

            float distanceToInitialPosition = Vector3.Distance(transform.position, initialBullPosition);

            if (distanceToInitialPosition <= 1.0f)
            {
                ResetCharge();
                isReturning = false;
            }
        }
        // 머리가 돌을 바라보게 합니다.
        if (targetRock != null && !isCharging && !isReturning)
        {
            transform.LookAt(new Vector3(targetRock.transform.position.x, transform.position.y, targetRock.transform.position.z));
        }
    }
    public void TakeDamage(float damage) // 데미지를 받는 메서드를 선언합니다.
    {
        health -= damage; // 체력을 감소시킵니다.
        if (health <= 0) // 체력이 0 이하일 경우
        {
            Destroy(gameObject); // 게임 오브젝트를 제거합니다.
        }
    }
    private void DetectRock() // 돌을 탐지하는 메서드를 선언합니다.
    {
        Collider[] rocks = Physics.OverlapSphere(transform.position, detectionRange, Rock);
        if (rocks.Length > 0 && rocks[0] != null)
        {
            targetRock = rocks[0].transform;
            lastRockPosition = targetRock.position; // 돌의 현재 위치를 저장
            isCharging = true; // 돌진 상태를 true로 설정합니다.
        }
    }
    private void ChargeTowardsRock() // 돌을 향해 돌진하는 메서드를 선언합니다.
    {
        Vector3 direction = (lastRockPosition - transform.position).normalized;
        direction.y = 0; // y축 값을 0으로 설정합니다.r
        float distanceToTarget = Vector3.Distance(lastRockPosition, transform.position);
        bullRigidbody.velocity = direction * chargeSpeed;
    }

    void ResetCharge()
    {
        lastChargeTime = 0f;
        chargeCount = 0;
        isCharging = false;
        if (targetRock != null)
        {
            transform.LookAt(new Vector3(targetRock.transform.position.x, transform.position.y, targetRock.transform.position.z));
        }
    }
    void OnCollisionEnter(Collision collision) // 충돌이 발생했을 때의 메서드를 선언합니다.
    {
        RockBase rock = collision.gameObject.GetComponent<RockBase>();

        if (rock != null && chargeCount < 1)
        {

            hasCharged = true;
            chargeCount++;
            isCharging = false; // 충돌 시 돌진 상태를 false로 설정합니다.
            isReturning = true; // 충돌 후 후퇴 상태를 true로 설정합니다.
            Rigidbody rockRigidbody = collision.gameObject.GetComponent<Rigidbody>();
            Rigidbody bullRigidbody = GetComponent<Rigidbody>();

            if (bullRigidbody != null)
            {
                bullRigidbody.velocity = Vector3.zero;
            }

            TakeDamage(1f);
            if (rockRigidbody != null)
            {
                Vector3 forceDirection = (targetRock.position - transform.position).normalized;
                rockRigidbody.velocity = Vector3.zero;
                rockRigidbody.AddForce(forceDirection * attackPower, ForceMode.VelocityChange);
                rockRigidbody.AddForce(Vector3.up * 10f, ForceMode.VelocityChange);

            }
            ResetCharge();
        }

    }

    private void ReturnToInitialPositionBackwards()
    {
        Vector3 direction = (initialBullPosition - transform.position).normalized;
        direction.y = 0;
        transform.rotation = Quaternion.LookRotation(-direction);
        transform.position += direction * walkSpeed * Time.deltaTime;

        float distanceToInitialPosition = Vector3.Distance(transform.position, initialBullPosition);
        if (distanceToInitialPosition <= 1.0f)
        {
            isReturning = false;
        }
    }
}