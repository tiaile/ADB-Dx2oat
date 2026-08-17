#!/bin/bash
# ============================================================
# 【给后来者】中文乱码说明与解决办法：
# 本脚本输出 UTF-8，而 Windows 控制台默认按 GBK 解码，
# 直接运行中文会显示成乱码（如"瀹夊崜..."），这只是显示问题，
# 不影响脚本功能。
#
# 在 PowerShell 里运行前先执行下面这一行，即可正常显示中文：
#   [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
#
# 完整运行示例：
#   cd D:\Nintendo\lib\ghostlock-app\adb
#   [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
#   D:\Nintendo\devkitPro\msys2\usr\bin\bash.exe -c "bash 安卓dax2oat编译检查.sh"
# ============================================================

# 批量检查安卓应用 dex2oat 处理状态
# 使用前确保已连接设备并开启 ADB 调试

# 设置 ADB 命令（适配 Windows）
ADB_CMD="./adb.exe"

echo "========================================"
echo "  安卓应用 dex2oat 状态批量检查工具"
echo "========================================"
echo "正在获取用户应用列表...\n"

# 初始化统计变量
total=0
compiled=0
uncompiled=0
only_verify=0

# 存储未处理的应用
declare -a uncompiled_apps

# 获取所有用户应用包名（过滤系统应用）
pkg_list=$($ADB_CMD shell pm list packages -3 | sed 's/package://g')

# 计算总应用数
total_apps=$(echo "$pkg_list" | wc -l)
current=0

# 遍历每个应用包名
for pkg in $pkg_list; do
    current=$((current+1))
    
    # 显示动态进度
    echo -ne "\r[$current/$total_apps] 检查应用: $pkg...    "
    
    # 获取编译状态和过滤规则
    compile_info=$($ADB_CMD shell dumpsys package $pkg 2>/dev/null | grep -E "compileFilter|status=|oatDir")
    
    # 判断编译状态
    if echo "$compile_info" | grep -qE "status=speed|status=odex|compileFilter=speed|compileFilter=speed-profile"; then
        compiled=$((compiled+1))
    elif echo "$compile_info" | grep -qE "compileFilter=quicken|compileFilter=verify"; then
        only_verify=$((only_verify+1))
    else
        uncompiled=$((uncompiled+1))
        # 获取应用名称并添加到未处理列表
        app_name=$($ADB_CMD shell pm dump $pkg 2>/dev/null | grep -A 1 "versionName" | head -n 1 | sed 's/versionName=//g' | tr -d '\r')
        uncompiled_apps+=("$pkg|$app_name")
    fi
done

echo -e "\n检查完成！"

# 输出汇总结果
echo -e "\n========================================"
echo "              检查结果汇总              "
echo "========================================"
echo "✅ 已完整处理（speed/speed-profile）：$compiled 个"
echo "⚠️  仅轻量验证（quicken/verify）：$only_verify 个"
echo "❌ 未处理（无 OAT）：$uncompiled 个"
echo "📊 总计检查用户应用：$total_apps 个"
echo "========================================"

# 输出未处理应用列表
if [ ${#uncompiled_apps[@]} -gt 0 ]; then
    echo -e "\n📋 未处理应用列表："
    echo "========================================"
    for app_info in "${uncompiled_apps[@]}"; do
        pkg=$(echo "$app_info" | cut -d'|' -f1)
        name=$(echo "$app_info" | cut -d'|' -f2)
        echo "• $name ($pkg)"
    done
    echo "========================================"
    
    # 询问用户是否要一键编译
    echo -e "\n是否一键编译这些未处理的应用？[Y/N]"
    read -r response
    
    # 转换为小写
    response=$(echo "$response" | tr '[:upper:]' '[:lower:]')
    
    if [ "$response" = "yes" ] || [ "$response" = "y" ]; then
        echo -e "\n🚀 开始编译未处理的应用..."
        echo "========================================"
        
        compiled_count=0
        failed_count=0
        
        for app_info in "${uncompiled_apps[@]}"; do
            pkg=$(echo "$app_info" | cut -d'|' -f1)
            name=$(echo "$app_info" | cut -d'|' -f2)
            
            echo -ne "正在编译: $name ($pkg)... "
            
            # 使用 speed 模式编译
            result=$($ADB_CMD shell cmd package compile -m speed $pkg 2>&1)
            
            if echo "$result" | grep -q "Success"; then
                echo "✅ 成功"
                compiled_count=$((compiled_count+1))
            else
                echo "❌ 失败"
                failed_count=$((failed_count+1))
            fi
        done
        
        echo "========================================"
        echo "✅ 编译成功: $compiled_count 个"
        echo "❌ 编译失败: $failed_count 个"
        echo "========================================"
        
        if [ $failed_count -eq 0 ]; then
            echo -e "\n🎉 所有应用编译完成！"
        else
            echo -e "\n⚠️  部分应用编译失败，请检查错误信息"
        fi
        
        # 添加编译限制提示
        echo -e "\n💡 提示：如果某些应用反复编译不成功，可能是该应用有系统编译限制"
        echo "   这种情况下应用仍以 verify 模式运行，属于正常现象"
    else
        echo -e "\n已取消编译操作"
    fi
else
    echo -e "\n✅ 所有应用都已处理完毕！"
fi