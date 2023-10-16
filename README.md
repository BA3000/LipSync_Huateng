# 口型文档

## 如何使用本工程

### 实时解析语音（旧版工程）

项目根目录下就是 Unity 工程，需要使用 Unity 2019.4.35 或者更高的版本打开。

PythonScript 保存的是深度模型，先运行 Unity 项目，后运行 Python 脚本：

``` shell
cd PythonScript/audio2face_pytorch
python ./src/demo/run_demo.py --only_bs_data
```

每次运行 Unity 预览前都需要先运行上述指令。

### 离线

目前工程是直接读取 Python 工程生成的 JSON 文件来播放口型的（有点类似烘焙的流程，Unity 只负责读取烘焙出来的动画），所以只需要运行 Unity 工程，然后按键盘 P 键即可看到效果。
