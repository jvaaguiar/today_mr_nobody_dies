﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum GodPower {Bomb, Turret, Trap};

public class GodMgr : MonoBehaviour
{
    public float BombMaximumSpeed = 1.0f;
    public float BombPowerCooldown = 1.5f;
    public float TurretPowerCooldown = 2f;
    public float TrapPowerCooldown = 2f;

    public float DelimiterX;
    public float DelimiterY;

    public GameObject PrefabBomb;
    public GameObject PrefabTrap;
    public GameObject PrefabTurret;
    public GameObject PrefabAngleIndicator;

    private GodPower CurrentPower = GodPower.Bomb;

    private bool MaySwitchPowers = true;

    // Bomb stuff
    private bool IsDraggingBomb = false;
    private GameObject DraggedBomb = null;
    private float LastBomb = 0.0f;

    // Turret stuff
    private bool IsPlacingTurret = false;
    private GameObject PlacedTurret = null;
    private float LastTurret = 0.0f;
    public float TurretAngleSteps = 0.0f;
    private GameObject AngleIndicator;

    // Trap stuff
    private float LastTrap = 0.0f;
        
    private Queue<Vector3> LastMousePositions;
    private Queue<float> LastStepsTime;

    // ======================================================================================
    private static Vector3 GetMouseWorldPos()
    {
        Vector3 Scale = new Vector3(1, 1, 0);
        Vector3 ZOffset = new Vector3(0, 0, SceneMgr.GlobalZ);
        return Vector3.Scale(
            Camera.main.ScreenToWorldPoint(Input.mousePosition),
            Scale
        ) + ZOffset;
    }

    // ======================================================================================
    private bool IsInGodRegion()
    {
        float ScaleX = Input.mousePosition.x / Screen.width;
        float ScaleY = Input.mousePosition.y / Screen.height;

        return (
            ScaleX < DelimiterX ||
            ScaleX > 1-DelimiterX ||
            ScaleY > 1-DelimiterY
        );
    }

    // ======================================================================================
    void Start ()
    {
        LastMousePositions = new Queue<Vector3>();
        LastStepsTime = new Queue<float>();

        LastBomb = -BombPowerCooldown;
        LastTrap = -TrapPowerCooldown;
        LastTurret = -TurretPowerCooldown;
    }

    // ======================================================================================
    void FixedUpdate() {

        if (GameMgr.IsPaused || SceneMgr.IsGameOver)
        {
            return;
        }

        UpdatePowersGUI();

        IsInGodRegion();

        if (MaySwitchPowers)
        {
            if (Input.GetButtonDown("Create Bomb"))
            {
                CurrentPower = GodPower.Bomb;
            }
            else if (Input.GetButtonDown("Create Turret"))
            {
                CurrentPower = GodPower.Turret;
            }
            else if (Input.GetButtonDown("Create Trap"))
            {
                CurrentPower = GodPower.Trap;
            }
        }

        if (CurrentPower == GodPower.Bomb)
        {
            if (!IsDraggingBomb && GameMgr.Timer > LastBomb + BombPowerCooldown && IsInGodRegion() && Input.GetButton("Fire1"))
            {
                MaySwitchPowers = false;
                IsDraggingBomb = true;

                DraggedBomb = Instantiate(
                    PrefabBomb,
                    GetMouseWorldPos(),
                    Quaternion.identity
                ) as GameObject;
            }

            if(!Input.GetButton("Fire1") || !IsInGodRegion())
            {
                if (IsDraggingBomb)
                {
                    IsDraggingBomb = false;
                    MaySwitchPowers = true;

                    LastBomb = GameMgr.Timer;

                    if (DraggedBomb)
                    {
                        Rigidbody BombPhysics = DraggedBomb.GetComponent<Rigidbody>();
                        BombPhysics.useGravity = true;
                        BombPhysics.isKinematic = false;

                        Vector3[] PositionVector = LastMousePositions.ToArray() as Vector3[];
                        float[] TimeArray = LastStepsTime.ToArray() as float[];

                        Vector3 ThrowVector = (
                            (PositionVector[PositionVector.Length - 1] - PositionVector[0]) /
                            (TimeArray[TimeArray.Length - 1] - TimeArray[0])
                        );

                        BombPhysics.velocity = ThrowVector.normalized * Mathf.Clamp(ThrowVector.magnitude, 0, BombMaximumSpeed);
                    }
                }
            }

            if (IsDraggingBomb)
            {
                Rigidbody BombPhysics = DraggedBomb.GetComponent<Rigidbody>();
                BombPhysics.position = GetMouseWorldPos();
                /*
                    Vector3 GoalVector = (GetMouseWorldPos() - BombPhysics.position);
                    BombPhysics.velocity = (
                    BombPhysics.velocity +
                    GoalVector.normalized * Time.fixedDeltaTime * BombAcceleration
                );*/
            }
        } else if (CurrentPower == GodPower.Turret)
        {
            if (Input.GetButton("Fire1"))
            {
                if (!IsPlacingTurret)
                {
                    if (GameMgr.Timer > LastTurret + TurretPowerCooldown)
                    {
                        MaySwitchPowers = false;
                        IsPlacingTurret = true;

                        PlacedTurret = Instantiate(
                            PrefabTurret,
                            GetMouseWorldPos(),
                            Quaternion.identity
                        ) as GameObject;

                        AngleIndicator = Instantiate(
                            PrefabAngleIndicator,
                            PlacedTurret.transform.position,
                            Quaternion.identity
                        ) as GameObject;
                    }
                }

                if (IsPlacingTurret)
                {
                    if (GetMouseWorldPos() == PlacedTurret.transform.position)
                    {
                        AngleIndicator.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                    }
                    else
                    {
                        float WinningAngle = 0.0f;
                        float MouseAngle = Vector3.Angle(Vector3.right, GetMouseWorldPos() - PlacedTurret.transform.position);

                        for (float angle = 0.0f; angle < 360.0f; angle += TurretAngleSteps)
                        {
                            if (Mathf.Abs(angle - MouseAngle) < Mathf.Abs(WinningAngle - MouseAngle))
                            {
                                WinningAngle = angle;
                            }
                        }

                        AngleIndicator.transform.rotation = Quaternion.Euler(0.0f, 0.0f, -WinningAngle);
                    }
                }
            }
            else
            {
                if (IsPlacingTurret)
                {
                    LastTurret = GameMgr.Timer;

                    if (AngleIndicator)
                    {
                        Destroy(AngleIndicator);
                    }

                    float WinningAngle = 0.0f;
                    float MouseAngle = Vector3.Angle(Vector3.right, GetMouseWorldPos() - PlacedTurret.transform.position);

                    for (float angle = 0.0f; angle < 360.0f; angle += TurretAngleSteps)
                    {
                        if (Mathf.Abs(angle - MouseAngle) < Mathf.Abs(WinningAngle - MouseAngle))
                        {
                            WinningAngle = angle;
                        }
                    }
                    
                    PlacedTurret.GetComponent<LaserTurret>().Activate(WinningAngle);

                    IsPlacingTurret = false;
                    MaySwitchPowers = true;
                    PlacedTurret = null;
                }
            }
        } else if (CurrentPower == GodPower.Trap)
        {
            if (Input.GetButtonDown("Fire1") && GameMgr.Timer > LastTrap + TrapPowerCooldown)
            {
                Instantiate(
                    PrefabTrap,
                    GetMouseWorldPos(),
                    Quaternion.identity
                );

                LastTrap = GameMgr.Timer;
            }
        }

        LastStepsTime.Enqueue(GameMgr.Timer);
        LastMousePositions.Enqueue(GetMouseWorldPos());
        if (LastMousePositions.Count > 5)
        {
            LastMousePositions.Dequeue();
            LastStepsTime.Dequeue();
        }
    }

    // ======================================================================================
    private void UpdatePowersGUI()
    {
        GUIMgr.BombSlider.SetSlider(1 - (GameMgr.Timer - LastBomb) / BombPowerCooldown);
        GUIMgr.TurretSlider.SetSlider(1 - (GameMgr.Timer - LastTurret) / TurretPowerCooldown);
        GUIMgr.TrapSlider.SetSlider (1 - (GameMgr.Timer - LastTrap) / TrapPowerCooldown);
    }
}
