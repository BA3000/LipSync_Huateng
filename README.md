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

首先需要使用脚本将录音文件（wav格式）转换为特殊格式的文本文件：

``` shell
cd PythonScript/audio2face_pytorch
python ./src/demo/run_demo.py --only_bs_data
```

注意，一定要在`PythonScript/audio2face_pytorch`下执行命令，因为脚本中写死的路径都是以此目录作为相对路径。

在生成了文件之后，再调用`PythonScript\audio2face_pytorch\src\utils\anim2Json.py`来转换即可，记得修改文件将`INPUT_FILE_PATH`修改为需要转换的文件路径（目前只能转换单个文件，如有需要再改成转换指定目录下所有文件）。

``` shell
cd PythonScript/audio2face_pytorch
python ./src/utils/anim2Json.py
```

转换完成之后将转换出来的 JSON 文件拖入到 Unity 工程下的 Resources 路径，即`Assets\Resources`，这个文件夹下已经有一个 `temp.json`，参考这个 JSON 即可。

随后将场景中角色身上挂载的 LipSync 脚本，修改此处变量为刚刚拖到 `Resources` 文件夹的 JSON 文件名字即可：

![20231017011746](https://images-1300215216.cos.ap-guangzhou.myqcloud.com/Blog/20231017011746.png)

如上图，就是指向了 `Resources` 文件夹下的 `temp.json` 文件。
