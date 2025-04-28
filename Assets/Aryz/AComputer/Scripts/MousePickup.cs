
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

public class MousePickup : UdonSharpBehaviour
{
    public MouseSimulator mouseSimulator;

    void Update()
    {
        // 获取物体与 VRC Pickup 之间的 Y 轴距离
        float yDistance = Mathf.Abs(mouseSimulator.transform.localPosition.y);

        // 计算旋转插值因子，y 越接近 0，旋转越接近 0，y 越大，旋转逐渐恢复
        float rotationFactor = Mathf.InverseLerp(0f, 0.2f, yDistance);

        // 设置物体的位置
        transform.position = mouseSimulator.transform.position;

        // 计算旋转：y 越接近 0，旋转越接近 0，y 越大，旋转逐渐恢复
        Quaternion targetRotation = Quaternion.Euler(mouseSimulator.transform.localRotation.eulerAngles);
        transform.localRotation = Quaternion.Lerp(Quaternion.identity, targetRotation, rotationFactor);
    }
}
