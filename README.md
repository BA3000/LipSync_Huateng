## 如何使用本工程

项目根目录下就是 Unity 工程，需要使用 Unity 2019.4.35 或者更高的版本打开。

PythonScript 保存的是深度模型，运行 Unity 项目前需要先运行 Python 脚本：

``` shell
cd src/demo
python run_demo.py --only_bs_data
```

每次运行 Unity 预览前都需要先运行上述指令。
