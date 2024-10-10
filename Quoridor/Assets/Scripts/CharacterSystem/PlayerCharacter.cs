using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCharacter : BaseCharacter
{
    public PlayerCharacter(CharacterController controller) : base(controller) { }

    public override void Attack()
    {
        base.Attack();
    }

    public override void Move()
    {
        base.Move();
    }

    public override void Ability()
    {
        base.Ability();
    }

    public override void Build()
    {
        base.Build();
    }

    public override void HealthRecovery(int recovery)
    {
        base.HealthRecovery(recovery);
    }

    public override void TakeDamage(BaseCharacter baseCharacter, int damage = 0)
    {
        base.TakeDamage(baseCharacter, damage);
    }

    //public void ResetPreview()
    //{

    //}

    //private void PlayerStart()
    //{
    //    wallStorage = GameObject.FindGameObjectWithTag("WallStorage");
    //    previewStorage = GameObject.FindGameObjectWithTag("PreviewStorage");

    //    for (int i = 0; i < movablePositions.Count; i++) // 플레이어 미리보기 -> 미리소환하여 비활성화 해놓기
    //    {
    //        playerPreviews.Add(controller.SetObjectToParent(playerPrefabs.playerPreview, previewStorage));
    //        playerPreviews[i].SetActive(false);
    //    }
    //    for (int i = 0; i < attackablePositions.Count; i++) // 플레이어 공격 미리보기 -> 미리소환하여 비활성화 해놓기
    //    {
    //        playerAttackPreviews.Add(controller.SetObjectToParent(playerPrefabs.attackPreview, previewStorage));
    //        playerAttackPreviews[i].SetActive(false);
    //    }
    //    //for (int i = 0; i < attackPositions.Count; i++) // 플레이어 공격 하이라이트 -> 미리소환하여 비활성화 해놓기
    //    //{
    //    //    playerAttackHighlights.Add(controller.SetObjectToParent(playerPrefabs.attackHighlight, previewStorage));
    //    //    playerAttackHighlights[i].SetActive(false);
    //    //}
    //    for (int i = 0; i < 81; i++) // 플레이어 능력 미리보기 -> 미리소환하여 비활성화 해놓기
    //    {
    //        playerAbilityPreviews.Add(controller.SetObjectToParent(playerPrefabs.attackPreview, previewStorage, new Vector3(i % 9 - 4, i / 9 - 4, 0) * GameManager.gridSize));
    //        //playerAbilityPreviews[i].transform.parent = previewStorage.transform;
    //        playerAbilityPreviews[i].SetActive(false);
    //    }
    //    for (int i = 0; i < 81; i++) // 플레이어 능력 하이라이트 -> 미리소환하여 비활성화 해놓기
    //    {
    //        playerAbilityHighlights.Add(controller.SetObjectToParent(playerPrefabs.attackHighlight, previewStorage, new Vector3(i % 9 - 4, i / 9 - 4, 0) * GameManager.gridSize));
    //        //playerAbilityHighlights[i].transform.parent = previewStorage.transform;
    //        playerAbilityHighlights[i].SetActive(false);
    //    }
    //    for (int i = 0; i < GameManager.Instance.playerMaxBuildWallCount; i++)
    //    {
    //        controller.SetObjectToParent(playerPrefabs.wall, wallStorage).SetActive(false);
    //    }
    //    playerWallPreview = controller.SetObjectToParent(playerPrefabs.wallPreview, null, position); // 플레이어 벽 미리보기 -> 미리소환하여 비활성화 해놓기
    //    playerWallPreview.SetActive(false);
    //}

    //// CharacterController에서 currentPlayer가 누군지에 따라 해당 BaseCharacter 스크립트안에 실행되는 playerUpdate 결정
    //// BaseCharacter에서 결정 X
    //private void PlayerUpdate()
    //{
    //    touchPosition = controller.touchPos;
    //    switch (controller.playerControlStatus)
    //    {
    //        case EPlayerControlStatus.Move:
    //            if (canMove) Move();
    //            else ResetPreview();
    //            break;
    //        case EPlayerControlStatus.Build:
    //            if (canBuild) Build();
    //            else ResetPreview();
    //            break;
    //        case EPlayerControlStatus.Attack:
    //            if (canAttack) Attack();
    //            else ResetPreview();
    //            break;
    //        // case GameManager.EPlayerControlStatus.Ability:
    //        //     if (abilityCount > 0) UseAbility();
    //        //     else ResetPreview();
    //        //     break;
    //        //case EPlayerControlStatus.Destroy:
    //        //    if (canDestroy) Destroy();
    //        //    else ResetPreview();
    //        //    break;

    //        default:
    //            break;
    //    }
    //}


    //private void SetPreviewPlayer()
    //{
    //    for (int i = 0; i < movablePositions.Count; i++)
    //    {
    //        bool[] result = HMPhysics.CheckRay(controller.currentCtrlCharacter.transform.position, (Vector2)movablePositions[i]);
    //        if (result[0])
    //        {
    //            playerPreviews[i].SetActive(false);
    //            continue;
    //        }
    //        if (result[1])
    //        {
    //            if (!result[2])
    //            {
    //                Debug.DrawRay(controller.currentCtrlCharacter.transform.position, (Vector2)movablePositions[i] * GameManager.gridSize, Color.green, 0.1f);
    //                playerPreviews[i].transform.position = controller.currentCtrlCharacter.transform.position + GameManager.ChangeCoord(movablePositions[i]);
    //                playerPreviews[i].SetActive(true);
    //            }
    //            else
    //            {
    //                Debug.DrawRay(controller.currentCtrlCharacter.transform.position, (Vector2)movablePositions[i] * GameManager.gridSize, Color.yellow, 0.1f);
    //            }
    //            continue;
    //        }
    //        else
    //        {
    //            Debug.DrawRay(controller.currentCtrlCharacter.transform.position, (Vector2)movablePositions[i] * GameManager.gridSize, Color.red, 0.1f);
    //        }
    //    }
    //}
}
