using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parabola : MonoBehaviour
{
    public GameObject point;
    private float initialHight = 0;                  //���߿�ʼ����ĳ�ʼ�߶�
    public float initialVelocity = 0;                //��ʼ�ٶ�
    private float velocity_Horizontal, velocity_Vertical;  //ˮƽ���ٶȺʹ�ֱ���ٶ�
    private float includeAngle = 0;                  //��ˮƽ����ļн�
    private float totalTime = 0;                     //�׳�����ص���ʱ��
    private float timeStep = 0;                      //ʱ�䲽��

    private LineRenderer line;
    [SerializeField] private float lineWidth = 0.07f;
    [SerializeField] private Material lineMaterial;
    private RaycastHit hits;

    [Range(2, 200)] public int line_Accuracy = 10;   //���ߵľ��ȣ��յ�ĸ���)
    private float grivaty = 9.8f;
    private int symle = 1;                           //ȷ�����µķ���
    private Vector3 parabolaPos = Vector3.zero;      //�����ߵ�����
    private Vector3 lastCheckPos, currentCheckPos;   //��һ���͵�ǰһ�����������
    private Vector3 checkPointPosition;              //����ķ�������
    private Vector3[] checkPointPos;                 //�������������
    private float timer = 0;                         //�ۼ�ʱ��
    private int lineCount = 0;

    private Transform startPoint;

    //GameObject point;
    private void Start()
    {
        startPoint = transform;
        //point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        if (!this.GetComponent<LineRenderer>())
        {
            line = this.gameObject.AddComponent<LineRenderer>();
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.material = lineMaterial;
        }
    }
    private void Update()
    {
        if (startPoint == null)
        {
            return;
        }
        Calculation_parabola();
    }
    private void Calculation_parabola()
    {
        velocity_Horizontal = initialVelocity * Mathf.Cos(includeAngle);
        velocity_Vertical = initialVelocity * Mathf.Sin(includeAngle);
        initialHight = Mathf.Abs(startPoint.transform.position.y);
        float time_1 = velocity_Vertical / grivaty;
        float time_2 = Mathf.Sqrt((time_1 * time_1) + (2 * initialHight) / grivaty);
        totalTime = time_1 + time_2;
        timeStep = totalTime / line_Accuracy;
        includeAngle = Vector3.Angle(startPoint.forward, Vector3.ProjectOnPlane(startPoint.forward, Vector3.up)) * Mathf.Deg2Rad;
        symle = (startPoint.position + startPoint.forward).y > startPoint.position.y ? 1 : -1;

        if (checkPointPos == null || checkPointPos.Length != line_Accuracy)
        {
            checkPointPos = new Vector3[line_Accuracy];
        }
        for (int i = 0; i < line_Accuracy; i++)
        {
            if (i == 0)
            {
                lastCheckPos = startPoint.position - startPoint.forward;
            }
            parabolaPos.z = velocity_Horizontal * timer;
            parabolaPos.y = velocity_Vertical * timer * symle + (-grivaty * timer * timer) / 2;
            currentCheckPos = startPoint.position + Quaternion.AngleAxis(startPoint.eulerAngles.y, Vector3.up) * parabolaPos;
            checkPointPosition = currentCheckPos - lastCheckPos;
            lineCount = i + 1;
            if (Physics.Raycast(lastCheckPos, checkPointPosition, out hits, checkPointPosition.magnitude + 3))
            {
                checkPointPosition = hits.point - lastCheckPos;
                checkPointPos[i] = hits.point;

                point.SetActive(true);
                point.transform.position = hits.point;
                point.transform.localScale = Vector3.one / 3;
                point.transform.GetComponent<MeshRenderer>().material.color = Color.red;
                if (hits.transform == null)
                {
                    point.SetActive(false);
                }
            }
            checkPointPos[i] = currentCheckPos;
            lastCheckPos = currentCheckPos;
            timer += timeStep;
        }
        line.positionCount = lineCount;
        line.SetPositions(checkPointPos);
        timer = 0;
    }

}
