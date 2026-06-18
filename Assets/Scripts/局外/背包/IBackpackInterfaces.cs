using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// ============================================================
// 背包模块系统对外接口定义
// 仅做声明，不做实现
// ============================================================

/// <summary>
/// 背包列表视图接口
/// </summary>
public interface IBackpackInventoryView
{
    /// <summary>重建模块列表</summary>
    void Rebuild(IReadOnlyList<IGridModule> modules, IGridModule selectedModule);

    /// <summary>模块被按下</summary>
    event Action<IGridModule> ModulePressed;

    /// <summary>模块被悬停</summary>
    event Action<IGridModule> ModuleHovered;

    /// <summary>模块悬停退出</summary>
    event Action ModuleHoverExited;
}

/// <summary>
/// 背包模块项 UI 接口
/// </summary>
public interface IBackpackModuleItem
{
    /// <summary>绑定模块数据与状态</summary>
    void Bind(IGridModule module, bool selected, bool isLoaded, Vector2 drawCellSize,
        Action<IGridModule> onPressed, Action<IGridModule> onHovered = null, Action onHoverExited = null);

    /// <summary>指针按下</summary>
    void OnPointerDown(PointerEventData eventData);

    /// <summary>指针进入</summary>
    void OnPointerEnter(PointerEventData eventData);

    /// <summary>指针退出</summary>
    void OnPointerExit(PointerEventData eventData);
}

/// <summary>
/// 网格模块定义接口
/// </summary>
public interface IGridModule
{
    /// <summary>模块是否已装载到网格</summary>
    bool IsLoaded { get; }

    /// <summary>应用模块到网格（注册战斗增益）</summary>
    void ApplyToBoard();

    /// <summary>从网格移除模块（注销战斗增益）</summary>
    void RemoveFromBoard();

    /// <summary>获取归一化后的单元格坐标列表</summary>
    void GetNormalizedCells(List<Vector2Int> results);

    /// <summary>获取模块尺寸（宽, 高）</summary>
    Vector2Int GetSize();

    /// <summary>获取归一化后的几何中心</summary>
    Vector2 GetNormalizedCenter();

    /// <summary>获取模块最大维度（宽或高）</summary>
    int GetMaxDimension();

    /// <summary>获取每个单元格的价格</summary>
    int GetPricePerCell();

    /// <summary>获取模块颜色（渐变起点色）</summary>
    Color ModuleColor { get; }

    /// <summary>获取渐变终点颜色</summary>
    Color GradientColorB { get; }

    /// <summary>克隆模块实例</summary>
    IGridModule Clone();

    /// <summary>顺时针旋转模块形状 90°，以指定归一化单元格为锚点</summary>
    void RotateClockwise(Vector2Int anchorNormalizedCell);
}

/// <summary>
/// 模块放置网格面板接口
/// </summary>
public interface IModulePlacementBoard
{
    /// <summary>构建/重建网格面板</summary>
    void BuildBoard();

    /// <summary>检查模块是否可放置在指定锚点</summary>
    bool CanPlace(IGridModule module, Vector2Int anchorCell);

    /// <summary>尝试放置模块</summary>
    bool TryPlace(IGridModule module, Vector2Int anchorCell);

    /// <summary>尝试从指定单元格拾取模块</summary>
    bool TryPickupModuleAt(Vector2Int cell, out IGridModule module);

    /// <summary>根据屏幕坐标获取对应的网格单元格</summary>
    bool TryGetCellFromScreenPoint(Vector2 screenPoint, Camera eventCamera, out Vector2Int cell);

    /// <summary>获取单元格中心在目标 RectTransform 中的局部坐标</summary>
    bool TryGetCellCenterInRect(RectTransform targetRect, Vector2Int cell, out Vector2 localPoint);

    /// <summary>获取单元格尺寸</summary>
    float GetCellSize();

    /// <summary>获取单元格步长（尺寸+间距）</summary>
    float GetCellStride();

    /// <summary>清空网格面板</summary>
    void ClearBoard();

    /// <summary>获取所有已放置模块</summary>
    void GetPlacedModules(List<ModulePlacementBoard.PlacedModuleState> results);

    /// <summary>尝试获取指定单元格上的模块</summary>
    bool TryGetModuleAtCell(Vector2Int cell, out IGridModule module);

    /// <summary>通知模块被悬停</summary>
    void NotifyModuleHovered(IGridModule module);

    /// <summary>通知模块悬停退出</summary>
    void NotifyModuleHoverExited();

    /// <summary>指针点击回调</summary>
    void OnPointerClick(PointerEventData eventData);

    /// <summary>单元格被点击</summary>
    event Action<Vector2Int> CellClicked;

    /// <summary>模块被悬停</summary>
    event Action<IGridModule> ModuleHovered;

    /// <summary>模块悬停退出</summary>
    event Action ModuleHoverExited;
}

/// <summary>
/// 模块放置控制器接口（背包系统主控）
/// </summary>
public interface IModulePlacementController
{
    /// <summary>模块总数</summary>
    int ModuleCount { get; }

    /// <summary>添加模块到背包</summary>
    void AddModuleToInventory(IGridModule module, bool autoSelect = false);

    /// <summary>获取所有拥有的模块</summary>
    IReadOnlyList<IGridModule> GetOwnedModules();

    /// <summary>尝试获取模块在拥有列表中的索引</summary>
    bool TryGetOwnedModuleIndex(IGridModule module, out int moduleIndex);
}
