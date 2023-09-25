using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ObstacleBase : MonoBehaviour
{
    //obstacle의 베이스

    //스테이터스
    public ObstacleStatus status;

    //건설시 position
    public float buildPositionY;

    //기본 데이터
    protected MeshFilter obstacleMeshFilter;
    protected Rigidbody obstacleRigidBody;
    protected Animator obstacleAnimator;
    protected Renderer obstacleRenderer;
    protected Material originMaterial;
    //타겟
    protected GameObject target;

    //사람
    public KJHObject kjhObjectPrefab;
    //현재체력
    protected float currHealth;

    //건설완성=오브젝트활성화
    protected bool isBuildComplete = false;
    public bool dragObstacle = false;

    public static readonly float BUILD_TIME = 5f;


    private void OnEnable()
    {

        MakePeople();
    }

    void MakePeople()
    {

        if (kjhObjectPrefab == null)
        {
            return;
        }
        int numberOfPeople = Random.Range(3, 10);
        Vector3 targetPosition = transform.position;
        for (int i = 0; i < numberOfPeople; i++)
        {
            //랜덤한 위치에 벡터 생성
            Vector3 randomOffset = new Vector3(Random.Range(-1.5f, 1.5f), 1f, Random.Range(-1.5f, 1.5f));

            //랜덤한 위치에 사람 생성
            Vector3 peoplePosition = targetPosition + randomOffset;
            KJHObject PeopleInstance = Instantiate(kjhObjectPrefab, peoplePosition, Quaternion.identity);
        }
    }

    //제일 하단 스크립트에서 해당 함수를 불러온다(ONENABLE)
    protected void StartBuild(float time)
    {
        //마테리얼 교체
        originMaterial = obstacleRenderer.material;
        obstacleRenderer.material = BuildManager.instance.white;

        StartCoroutine(BuildRoutine(time));
    }

    //일정 시간동안 대기하는 COROUTINE
    protected IEnumerator BuildRoutine(float buildTime)
    {
        float currTime = 0;
        while (currTime < buildTime)
        {
            yield return Time.deltaTime;
            currTime += Time.deltaTime;
        }

        isBuildComplete = true;
        obstacleRenderer.material = originMaterial;
        MakePeople();
    }

    //맵에 Build
    public virtual ObstacleBase Build(Vector3 position, Quaternion rotate)
    {
        //오브젝트 생성
        ObstacleBase obstacle = Instantiate(this, position, rotate);

        //스케일 변경
        obstacle.transform.localScale = obstacle.transform.localScale;

        //버튼 데이터 변경
        GameObject unitButton = ResourceManager.Instance.FindUnitGameObjById(status.Id);
        unitButton.GetComponent<CreateButton>().buildCount += 1;
        UIManager.uiManager.RePrintUnitCount(status.Id);

        return obstacle;
    }

    //초기화
    protected virtual void Init()
    {
        status = new ObstacleStatus(status);// Instantiate(status);
        obstacleMeshFilter = GetComponent<MeshFilter>();
        obstacleRigidBody = GetComponent<Rigidbody>();
        obstacleAnimator = GetComponent<Animator>();
        obstacleRenderer = GetComponentInChildren<Renderer>();
        currHealth = status.Health;
    }

    //타겟 서치
    //아마 상위에 없어도 될거로 추정됨
    protected virtual void SearchTarget() { }

    //죽음
    protected virtual void Dead() { }

    //공격 활성화
    protected virtual void ActiveAttack() { }
    protected virtual void DeactiveAttack() { }

}