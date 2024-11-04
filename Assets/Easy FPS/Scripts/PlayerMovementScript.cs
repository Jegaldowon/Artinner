using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementScript : MonoBehaviour
{
    Rigidbody rb;

    [Tooltip("플레이어의 현재 속도")]
    public float currentSpeed;
    [Tooltip("플레이어의 카메라를 지정하십시오")]
    [HideInInspector] public Transform cameraMain;
    [Tooltip("플레이어가 점프할 때 이동하는 힘")]
    public float jumpForce = 500;
    [Tooltip("플레이어 내부의 카메라 위치")]
    [HideInInspector] public Vector3 cameraPosition;

    /*
     * 플레이어의 리지드바디(Rigidbody) 컴포넌트를 가져옵니다.
     * 그리고 플레이어 자식 트랜스폼에서 메인 카메라를 가져옵니다.
     */
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cameraMain = transform.Find("Main Camera").transform;
        bulletSpawn = cameraMain.Find("BulletSpawn").transform;
        ignoreLayer = 1 << LayerMask.NameToLayer("Player");
    }
    private Vector3 slowdownV;
    private Vector2 horizontalMovement;

    /*
     * 근접 공격을 위한 레이캐스트와 입력 이동 처리를 수행합니다.
     */
    void FixedUpdate()
    {
        RaycastForMeleeAttacks();
        PlayerMovementLogic();
    }

    /*
     * 입력에 따라 힘을 추가하며 속도가 설정된 최대치를 넘으면 속도를 제한합니다.
     * 키를 놓으면 서서히 감속합니다.
     */
    void PlayerMovementLogic()
    {
        currentSpeed = rb.velocity.magnitude;
        horizontalMovement = new Vector2(rb.velocity.x, rb.velocity.z);
        if (horizontalMovement.magnitude > maxSpeed)
        {
            horizontalMovement = horizontalMovement.normalized * maxSpeed;
        }
        rb.velocity = new Vector3(horizontalMovement.x, rb.velocity.y, horizontalMovement.y);
        if (grounded)
        {
            rb.velocity = Vector3.SmoothDamp(rb.velocity, new Vector3(0, rb.velocity.y, 0), ref slowdownV, deaccelerationSpeed);
        }

        if (grounded)
        {
            rb.AddRelativeForce(Input.GetAxis("Horizontal") * accelerationSpeed * Time.deltaTime, 0, Input.GetAxis("Vertical") * accelerationSpeed * Time.deltaTime);
        }
        else
        {
            rb.AddRelativeForce(Input.GetAxis("Horizontal") * accelerationSpeed / 2 * Time.deltaTime, 0, Input.GetAxis("Vertical") * accelerationSpeed / 2 * Time.deltaTime);
        }

        /*
         * 미끄러지는 문제를 여기서 해결합니다.
         */
        if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0)
        {
            deaccelerationSpeed = 0.5f;
        }
        else
        {
            deaccelerationSpeed = 0.1f;
        }
    }

    /*
     * 점프를 처리하고 힘과 소리를 추가합니다.
     */
    void Jumping()
    {
        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            rb.AddRelativeForce(Vector3.up * jumpForce);
            if (_jumpSound)
                _jumpSound.Play();
            else
                print("점프 소리가 없습니다.");
            _walkSound.Stop();
            _runSound.Stop();
        }
    }

    /*
     * 다른 작업을 호출하는 업데이트 루프입니다.
     */
    void Update()
    {
        Jumping();
        Crouching();
        WalkingSound();
    }

    /*
     * 플레이어가 땅에 있는지 확인하고 속도에 따라 소리를 재생합니다.
     */
    void WalkingSound()
    {
        if (_walkSound && _runSound)
        {
            if (RayCastGrounded())
            {
                if (currentSpeed > 1)
                {
                    if (maxSpeed == 3)
                    {
                        if (!_walkSound.isPlaying)
                        {
                            _walkSound.Play();
                            _runSound.Stop();
                        }
                    }
                    else if (maxSpeed == 5)
                    {
                        if (!_runSound.isPlaying)
                        {
                            _walkSound.Stop();
                            _runSound.Play();
                        }
                    }
                }
                else
                {
                    _walkSound.Stop();
                    _runSound.Stop();
                }
            }
            else
            {
                _walkSound.Stop();
                _runSound.Stop();
            }
        }
        else
        {
            print("걷기와 달리기 소리가 없습니다.");
        }
    }

    /*
     * 땅이 울퉁불퉁한 경우를 대비해 바닥을 확인하기 위해 아래쪽으로 레이캐스트합니다.
     * 이 메서드는 플레이어가 땅에 제대로 닿았는지 확인합니다.
     */
    private bool RayCastGrounded()
    {
        RaycastHit groundedInfo;
        if (Physics.Raycast(transform.position, transform.up * -1f, out groundedInfo, 1, ~ignoreLayer))
        {
            Debug.DrawRay(transform.position, transform.up * -1f, Color.red, 0.0f);
            return groundedInfo.transform != null;
        }
        return false;
    }

    /*
     * 플레이어가 웅크리기 상태를 토글하면 크기를 변경하여 웅크리기 모션을 보이게 합니다.
     */
    void Crouching()
    {
        if (Input.GetKey(KeyCode.C))
        {
            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(1, 0.6f, 1), Time.deltaTime * 15);
        }
        else
        {
            transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(1, 1, 1), Time.deltaTime * 15);
        }
    }

    [Tooltip("달성하고자 하는 최대 속도")]
    public int maxSpeed = 5;
    [Tooltip("숫자가 클수록 빠르게 멈춥니다")]
    public float deaccelerationSpeed = 15.0f;

    [Tooltip("전진 및 후진 이동 시 적용되는 힘")]
    public float accelerationSpeed = 50000.0f;

    [Tooltip("플레이어가 땅에 닿아 있는지 여부를 나타냅니다.")]
    public bool grounded;

    /*
     * 플레이어가 지면과 60도 이하의 각도로 접촉하고 있으면 grounded를 true로 설정합니다.
     */
    void OnCollisionStay(Collision other)
    {
        foreach (ContactPoint contact in other.contacts)
        {
            if (Vector2.Angle(contact.normal, Vector3.up) < 60)
            {
                grounded = true;
            }
        }
    }

    /*
     * 충돌 종료 시 grounded를 false로 설정합니다.
     */
    void OnCollisionExit()
    {
        grounded = false;
    }

    RaycastHit hitInfo;
    private float meleeAttack_cooldown;
    private string currentWeapo;

    [Tooltip("'Player' 레이어를 여기에 설정하십시오")]
    [Header("사격 속성")]
    private LayerMask ignoreLayer;

    Ray ray1, ray2, ray3, ray4, ray5, ray6, ray7, ray8, ray9;
    private float rayDetectorMeeleSpace = 0.15f;
    private float offsetStart = 0.05f;

    [Tooltip("총알 생성 위치로 사용되는 BulletSpawn 오브젝트를 지정하십시오.")]
    [HideInInspector]
    public Transform bulletSpawn;

    /*
     * 9개의 레이를 다른 방향으로 쏘는 메서드입니다. (씬 탭에서 서로 다른 색상의 9개 레이를 확인할 수 있습니다.)
     * 전방의 적을 넓게 감지하여 근접 공격의 감지 범위를 넓히는 데 사용됩니다.
     * 마지막으로 수행된 근접 공격 이후 쿨다운 시간을 체크합니다.
     */


    public bool been_to_meele_anim = false;

    private void RaycastForMeleeAttacks()
    {
        // 근접 공격 쿨다운을 감소시킵니다.
        if (meleeAttack_cooldown > -5)
        {
            meleeAttack_cooldown -= 1 * Time.deltaTime;
        }

        // 현재 장착한 무기가 총기인지 확인합니다.
        if (GetComponent<GunInventory>().currentGun)
        {
            if (GetComponent<GunInventory>().currentGun.GetComponent<GunScript>())
                currentWeapo = "gun";
        }

        // 중앙 행의 Ray를 정의합니다.
        ray1 = new Ray(bulletSpawn.position + (bulletSpawn.right * offsetStart), bulletSpawn.forward + (bulletSpawn.right * rayDetectorMeeleSpace));
        ray2 = new Ray(bulletSpawn.position - (bulletSpawn.right * offsetStart), bulletSpawn.forward - (bulletSpawn.right * rayDetectorMeeleSpace));
        ray3 = new Ray(bulletSpawn.position, bulletSpawn.forward);

        // 상단 행의 Ray를 정의합니다.
        ray4 = new Ray(bulletSpawn.position + (bulletSpawn.right * offsetStart) + (bulletSpawn.up * offsetStart), bulletSpawn.forward + (bulletSpawn.right * rayDetectorMeeleSpace) + (bulletSpawn.up * rayDetectorMeeleSpace));
        ray5 = new Ray(bulletSpawn.position - (bulletSpawn.right * offsetStart) + (bulletSpawn.up * offsetStart), bulletSpawn.forward - (bulletSpawn.right * rayDetectorMeeleSpace) + (bulletSpawn.up * rayDetectorMeeleSpace));
        ray6 = new Ray(bulletSpawn.position + (bulletSpawn.up * offsetStart), bulletSpawn.forward + (bulletSpawn.up * rayDetectorMeeleSpace));

        // 하단 행의 Ray를 정의합니다.
        ray7 = new Ray(bulletSpawn.position + (bulletSpawn.right * offsetStart) - (bulletSpawn.up * offsetStart), bulletSpawn.forward + (bulletSpawn.right * rayDetectorMeeleSpace) - (bulletSpawn.up * rayDetectorMeeleSpace));
        ray8 = new Ray(bulletSpawn.position - (bulletSpawn.right * offsetStart) - (bulletSpawn.up * offsetStart), bulletSpawn.forward - (bulletSpawn.right * rayDetectorMeeleSpace) - (bulletSpawn.up * rayDetectorMeeleSpace));
        ray9 = new Ray(bulletSpawn.position - (bulletSpawn.up * offsetStart), bulletSpawn.forward - (bulletSpawn.up * rayDetectorMeeleSpace));

        // Ray를 시각적으로 디버그합니다.
        Debug.DrawRay(ray1.origin, ray1.direction, Color.cyan);
        Debug.DrawRay(ray2.origin, ray2.direction, Color.cyan);
        Debug.DrawRay(ray3.origin, ray3.direction, Color.cyan);
        Debug.DrawRay(ray4.origin, ray4.direction, Color.red);
        Debug.DrawRay(ray5.origin, ray5.direction, Color.red);
        Debug.DrawRay(ray6.origin, ray6.direction, Color.red);
        Debug.DrawRay(ray7.origin, ray7.direction, Color.yellow);
        Debug.DrawRay(ray8.origin, ray8.direction, Color.yellow);
        Debug.DrawRay(ray9.origin, ray9.direction, Color.yellow);

        // 근접 공격 상태 확인 및 애니메이션 처리
        if (GetComponent<GunInventory>().currentGun)
        {
            if (GetComponent<GunInventory>().currentGun.GetComponent<GunScript>().meeleAttack == false)
            {
                been_to_meele_anim = false;
            }
            if (GetComponent<GunInventory>().currentGun.GetComponent<GunScript>().meeleAttack == true && been_to_meele_anim == false)
            {
                been_to_meele_anim = true;
                StartCoroutine("MeeleAttackWeaponHit");
            }
        }
    }

    /*
     * 근접 공격 애니메이션이 처음으로 트리거될 때 호출되는 메서드입니다.
     * 목표를 탐색하고 데미지를 입힙니다.
     */
    IEnumerator MeeleAttackWeaponHit()
    {
        if (Physics.Raycast(ray1, out hitInfo, 2f, ~ignoreLayer) || Physics.Raycast(ray2, out hitInfo, 2f, ~ignoreLayer) || Physics.Raycast(ray3, out hitInfo, 2f, ~ignoreLayer)
            || Physics.Raycast(ray4, out hitInfo, 2f, ~ignoreLayer) || Physics.Raycast(ray5, out hitInfo, 2f, ~ignoreLayer) || Physics.Raycast(ray6, out hitInfo, 2f, ~ignoreLayer)
            || Physics.Raycast(ray7, out hitInfo, 2f, ~ignoreLayer) || Physics.Raycast(ray8, out hitInfo, 2f, ~ignoreLayer) || Physics.Raycast(ray9, out hitInfo, 2f, ~ignoreLayer))
        {

            // 타겟이 "Dummie" 태그를 가지고 있을 경우
            if (hitInfo.transform.tag == "Dummie")
            {
                Transform _other = hitInfo.transform.root.transform;
                if (_other.transform.tag == "Dummie")
                {
                    print("더미를 맞췄습니다.");
                }
                InstantiateBlood(hitInfo, false);
            }
        }
        yield return new WaitForEndOfFrame();
    }

    [Header("근접 공격에 대한 피 효과")]
    RaycastHit hit; // 타격 정보 저장
    [Tooltip("혈흔 효과 파티클을 넣으세요.")]
    public GameObject bloodEffect; // 피 효과 프리팹

    /*
     * 적을 타격했을 때 호출되며, Raycast 타격 정보와 해당 위치에 피 효과를 생성합니다.
     */
    void InstantiateBlood(RaycastHit _hitPos, bool swordHitWithGunOrNot)
    {
        if (currentWeapo == "gun")
        {
            GunScript.HitMarkerSound();

            if (_hitSound)
                _hitSound.Play();
            else
                print("타격 소리가 없습니다.");

            if (!swordHitWithGunOrNot)
            {
                if (bloodEffect)
                    Instantiate(bloodEffect, _hitPos.point, Quaternion.identity);
                else
                    print("피 효과 프리팹이 설정되지 않았습니다.");
            }
        }
    }
    private GameObject myBloodEffect;

    [Header("플레이어 사운드")]
    [Tooltip("점프 시 재생되는 사운드입니다.")]
    public AudioSource _jumpSound;
    [Tooltip("무기를 성공적으로 재장전했을 때의 사운드입니다.")]
    public AudioSource _freakingZombiesSound;
    [Tooltip("총알이 타격 시 나는 소리입니다.")]
    public AudioSource _hitSound;
    [Tooltip("플레이어가 걷기 시 나는 소리입니다.")]
    public AudioSource _walkSound;
    [Tooltip("플레이어가 달리기 시 나는 소리입니다.")]
    public AudioSource _runSound;
    
}
