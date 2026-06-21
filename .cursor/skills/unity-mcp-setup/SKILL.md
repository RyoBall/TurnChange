---
name: unity-mcp-setup
description: TurnChange 项目 Unity MCP 连接与验证。当用户询问 MCP 配置、Unity 连接失败、或需要读 Console/编译错误时使用。
---

# Unity MCP 一次性连接（Unity 侧）

Cursor 侧 MCP 已配置为 **stdio**（见 `.cursor/mcp.json`）。一键重配：`.cursor/configure-unity-mcp.ps1`

若 HTTP 启动失败，stdio 更稳定（Cursor 自行启动 `uvx`，无需 Unity HTTP 服务器）。

## Unity 中安装 MCP for Unity（仅需一次）

1. 打开 Unity 2022.3 并加载本项目
2. **Window → Package Manager → + → Add package from git URL**
3. 粘贴：
   ```
   https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main
   ```
4. 导入后打开 **Window → MCP for Unity** 向导
5. 确认 Python 3.10+ 与 `uv` 已安装（向导会引导）
6. 点击 **Configure Selected**，勾选 **Cursor**
7. 在 Cursor：**Settings → MCP**，确认 `unityMCP` 已启用

## 验证连接

1. Unity Editor 保持打开
2. MCP for Unity 状态面板显示 **Connected**
3. 在 Cursor Agent 中尝试：
   - 「列出 Assets/Scripts 下的主要 Manager 类」
   - 「读取 Unity Console 最近的编译错误」

## 故障排除

| 症状 | 处理 |
|------|------|
| Cursor 连不上 | 确认 Unity 开着；MCP 服务器在 8080 端口；重启 Cursor |
| Bridge 未连接 | Window → MCP for Unity 查看状态；重启 Unity |
| uv 未找到 | 安装 Python 3.10+ 后运行 `pip install uv` 或按向导提示 |

## 分工

- **Cursor**: 写/改 C#、搜索代码、架构分析
- **Unity MCP**: Console、编译错误、Play Mode、场景查询
- **Unity Editor**: Prefab 布局、Inspector 拖引用、动画/特效调参
