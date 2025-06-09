<h2 align="center">Deconstruction</h2>

<a href="https://gitee.com/none_9_0/xeric-library2"> 发布页面</a>

## 简介

unity通用库插件，现已转为unity 6000版本。

此插件为XericDigitalTwinTool插件库的前置依赖项。与MateriaLibrary材质库一起使用更好哦！

单独使用可能会遇到编译报错，可以直接删除报错指向的文件。造成报错的原因可能是版本不兼容，依赖项uid错误等等。如果能力足够，请自行解决。

webgl下部分内容为了兼容性可能与本体有所区别，注意辨别

## 软件架构

* CentralizeLog: 小日志，unity本身的日志在调用时开销巨大，所以提供一个小日志的类型保存临时的日志内容。
* Deconstruction: 通过数学公式对线进行描述，进行标准的CAD线路参数计算，并支持在线路上进行导航，合理性计算等
* RescissionRework: 撤销重做 🕑
* SerializerHelper: 序列化库
* XericLibrary: 实用性unity宏库，包含一些实用扩展用法。



----
----
## XericLibrary （主要核心库）

XericLibrary 部分由两个dll组成，一个是 Xeric Library，另一个是 Xeric Library Editor 。

----
### 宏库

----
#### 特性库

* CanFind$
    其中包含多个特性类，用于查找类，字段，属性，方法等项目。

* ReName
    重命名特性，在没有安装odin插件的旧版本引擎上可以直接替换细节面板中的参数名称，不过由于无法和odin兼容，所以odin的功能会覆盖掉这个特性。  
    主要作用是提供属性标签查找功能。

#### 协程宏
#### 调试宏 
#### 模型网格宏
#### 机器标识宏
#### 摄像机宏
#### 颜色宏 (MacorColor)
#### 常数宏 (MacroConst)
#### 曲线宏 (MacroCurve)
#### 枚举宏 (MacroEnum)

枚举宏包含一个缓存封装器，可以通过<code>MacroEnum\<enum\></code>来定义。  
定义枚举结构封装将允许像列表一样访问这个枚举，或将枚举实例赋予，通过枚举封装器来操作枚举值。  

#### 方程宏
#### 事件宏
#### 文件宏 (MacroFile)

文件宏包含预设常用的文件写入，读取操作，并自动解决文件不存在的问题，通过返回布尔状态来快速判断解决。

原始api中，仅包含一层简单的c#文件处理封装，无法适配unity其他平台情况，这些api已经被标记为过时，但你仍然可以继续使用他们。  
新版中，添加了对多平台的支持：

* 增加了<code>CrossPlatformFileHandle</code>文件句柄，用于处理多平台的文件写入，加载的基类。
	使用时，通过<code>CrossPlatformFileHandle.ActiveHandle</code>来获得当前平台自动对应的文件句柄，其中包括了一组同步和一组异步的读写方法，来对目标地址进行访问。  
	下面是一个示例：  
	```
	// ==== 同步读取 ==== 
	CrossPlatformFileHandle.ActiveFileHandle.ReadTextFromFile("absolute file path", out string content);

	// ==== 同步写入 ==== 
	CrossPlatformFileHandle.ActiveFileHandle.WriteTextIntoFile("absolute file path", string content);

	// ==== 异步读取 ==== 
	var isCompleteRead = await CrossPlatformFileHandle.ActiveFileHandle.AsyncReadTextFromFile("absolute file path", 
	s => 
	{
		// 成功读取文件
	}, 
	s => 
	{
		// 读取失败，原因是 s
		Debug.ErrorLog(s);
	});

	// ==== 异步写入 ==== 
	var isCompleteWrite = await CrossPlatformFileHandle.ActiveFileHandle.AsyncWriteTextIntoFile("absolute file path", 
	null, // 成功写入文件,但是无需处理
	null); // 写入失败，但是无需处理

	// 异步方法包含回调函数处理，也可通过返回布尔值判断文件写入是否成功。
	```
	同时，文件句柄也提供了相对路径拼接：
	```
	// 返回steamingAssets下的myFile.json文件路径
	CrossPlatformFileHandle.ActiveFileHandle.CombineStreamingAssets("myFile.json");
	```

* 添加了<code>XericSerializerFormatter</code>序列化器，用于通配处理多种序列化器目标。
	目前默认提供了 Json格式化 和 bin格式化 。  
	下面展示如何使用：
	```
	List<int> obj = new List<int>() { 1, 2, 3};
	// 序列化目标
	var content = JsonSerializerFormatter.formatter.Serializer(obj);
	// 反序列化目标
	obj = JsonSerializerFormatter.Deserializer.Serializer(content);
	```
	
	为了方便使用，文件句柄支持填入序列化器来实现更多的格式：
	```
	// 类型太长了可以使用声名简化
	using static XericLibrary.Runtime.MacroLibrary.MacroFile.CrossPlatformFileHandle;
	using static XericLibrary.Runtime.MacroLibrary.MacroFile.JsonSerializerFormatter;

	ActiveFileHandle.AsyncWriteXIntoFile(
		ActiveFileHandle.CombineStreamingAssets("myFile.json"), 
		uriConfig, null, s => Debug.LogError(s), formatter);
	```

----
#### 窗口宏 (MacroForm)

提供User32.dll中部分函数的功能。  

* 获取当前进程PID：<code>PID</code>
* 通过句柄查找窗口：<code>FindWindow</code>
* 控制窗口化状态：<code>ShowWindow</code>
* 枚举显示器信息：<code>GetDisplayMonitorsInfo</code>

----
#### 步进宏
#### 按键宏 (MacroKey)

预设了一些判断较复杂按键状态的逻辑。

对于外部的常规逻辑，可以使用扩展的InsertKey逻辑。  
为了使用这些逻辑，需要定义一个KeyFlippingStampSheetStyle(简称KfsStyle)。  
```
KeyCode.A.AsKfsStyle();
```
将这个作为按键索引，传入扩展的状态检查事件一同执行更新，即可做出判断。
这会返回一个KeyFlippingStampSheet，里面包含当前按键状态的布尔值，以及按键判断时触发的委托事件。

* 交叉动作与延迟触发长按  
	```
	var keySheet =  Input.GetMouseButtonDown(0).InsertKeyStateCmet(KeyCode.A.AsKfsStyle());
	```
	使用IsPressed判断是否单击按下，如果是双击，第一次点击时当前帧还是会返回单击事件。  
	使用IsReleased判断刚刚的单击是否释放，如果上一次是长按状态，那么这个释放事件不会触发。
	使用IsDoubleClick判断是否双击，双击事件会出现在第二次点击间隔小于KfsStyle中设置的双击间隔时。  
	使用IsFirstLongPressed判断是否长按，当一次按下后不释放时间大于KfsStyle中设置的长按间隔时出现一次。
	使用IsLongPressedState判断是否长按，当一次按下后不释放时间大于KfsStyle中设置的长按间隔时出现。
	使用IsLongReleased判断刚刚长按是否释放。

* 坐标保持触发
	```
	var keySheet =  Input.GetMouseButtonDown(0).InsertKeyStatePt(KeyCode.A.AsKfsStyle());
	```
	与上述功能一致，但可以使用拖拽事件：
	使用IsChangeDraggingFromLongPressed判断长按事件是否转为拖拽事件。
	使用IsBeginDragging判断当前是否开始拖拽。
	使用IsDragging判断当前是否正在拖拽。
	使用IsDropping判断当前是否正在丢弃。
	
	在拖拽期间，长按状态将继续触发，但长按事件将转为拖拽事件，长按事件。
	拖拽结束后，如果以拖拽结束，但以长按开始，那么长按结束事件也会触发。

* 光标坐标计算：
	提供诸如类似<code>GetRectangleRelativelyPoint</code>的扩展方法，将传入的光标坐标计算为相对坐标。

----
#### 列表宏 (MacroMath)

* FisherYatesShuffle: 洗牌算法，返回一个索引映射数组，同时也提供一个线程安全的蓄水池洗牌算法。
* GetRandom: 从列表中随机获得1个或多个对象（洗牌算法）。
* MakeIEnumberable: 将一个对象制作成枚举器。
* LandedIndex: 从迭代器中抽出索引下的成员。
* SelectConvert: 从非泛型迭代器转为泛型迭代器。
* ForEachDo: 迭代器执行。
* Adjoint: 迭代器拼接一个索引。
* SubSegment: 仅迭代开始索引后一段数量范围内的成员。
* Startup: 在当前迭代器之前拼接一个或多个成员（迭代器）
* Endeavor: 在当前迭代器之后拼接一个或多个成员（迭代器）
* Join: 将当前迭代器加入到字符串，可以设置如何开头，结尾，成员分割符。
* Merge: 将当前迭代器合并，可以实现类似sum的操作。
* EqualitySet: 比较两个迭代器里的成员是否相等。
* Shift: 偏移列表，也可以使用<code>RingShift</code>,<code>CyclicShift</code>循环偏移。
* 包含针对<code>IList<KeyValuePair<TKey, TValue>></code>的<code>Add</code>，<code>TryAdd</code>，<code>AddUnique</code>，<code>Remove</code>，<code>Modify</code>，<code>Contains</code>，<code>IndexOf</code>，<code>GetValue</code>，<code>GetKey</code>，<code>GetKeys</code>，<code>GetValues</code>方法。
* BinarySearch: 二分法遍历
* FindClosestIndexByBinarySearch: 基于浮点数的二分法查找方法。
* IsIndexGreaterZeroLessCount: 
* AddByIndex: 在索引处添加目标，如果索引不存在，则用default填充；如果目标已经存在，就替换。
* TryDoAtIndex: 尝试在索引处执行些什么，如果索引不存在，则什么都不做。
* ReplaceList: 使用一个列表替换另一个列表，允许通过自定义增删改方法修改（和DiscrepancyOrder作用几乎一致，不过这个会因为条件不满足而产生报错）。
* DiscrepancyOrder: 列表顺序差异，从源到目标列表的差异会通过委托开放，允许自定义删除，增加，替换来完成修改。
* TryGetAtIndex: 尝试读取索引处的成员。
* ForceDoAtIndex: 强制在索引处执行些什么，不论成员是否存在，永远都会执行委托。
* ForceGetAtIndex: 强制在索引处获取成员。
* 
----
#### 数学宏 (MacroMath)

* RandomFloat/Vector/Matrix：通过一个随机生成器快速获得随机值。
* Pmod：在正数空间上对值取余，负数情况下仍然会保持与正数相同的曲线形状。
* RoundToAny：将一个矢量四舍五入到任意的倍数，比如4.8舍入3.5得到3.5，6.2则得到7
* MinPositive：获取正数空间上的最小值。
* SmoothStep：获取t在(a,b)之间的埃尔米特过度
* LinearStep：获取t在(a,b)之间的线性过度
* RoundBasedOnMinimumDifference：将一个值根据最小的差值进行四舍五入
* DiscardLeastSignificantDecimal：舍弃不重要的小数位
* ClampWrapAngle：钳制角度
* GetNumberOfDecimalsForMinimumDifference：获取最小插值的小数个数
* CosineSimilarity：余弦相似度
* FibonacciRecursive：斐波那契迭代方式计算（会产生大量循环）
* FastInvSqrt：快速平方根倒数
* IsPrimeNumberRw：检查一个数是否是质数（这是一个娱乐用法，利用正则表达式判断是否为质数，性能消耗较高）
* GetCirclePoints：获取圆上的点
* GetRayIntersectionCircle：获取切于圆的线两点坐标
* GetHeightOfRoundArch：获取圆拱高度
* GetListFormAToB：获取从A到B的列表
* LerpTransform：创建lerp矩阵
* 
----
#### 消息库 (MacroMessage)

预设一些消息处理流程。

* 全局业务分发器，消息入口Entry。  

	特点是需要通过指定消息入口(<code>RedirectReceiverMessage</code>)和出口(<code>RedirectReceiver</code>)来中转消息。  
	完成后消息发送时会被进行一次包装，将带有入口名称的内容一同发送；而在消息处理的另一端，也需要用相同的逻辑处理这个包，就能获得这个包来自哪，并自动定位到对应的预定义委托上进行处理。
	下面是一个接收示例:
	```
	// 声名一个消息处理器委托
	// 接收器形参可以是任何可以序列化的结构，或是无形参接收器。
	public MacroMessage.ReceiverObject<int> myFunc;
	public void Awake()
    {
		// 初始化函数中注册这个委托，并赋予一个名称用于标识
        myFunc.RedirectReceiver(nameof(myFunc));
        myFunc += Func;
    }
	// 处理消息
    public void Func(int number)
    {
        // 消息处理
    }
	```
	下面是一个发送示例：
	```
	// 在收到文本消息后，立刻发送给重定向器
	void OnMessage(string message)
	{
		MacroMessage.RedirectReceiverMessage(message);
	}
	```
	最后，需要定义何时处理消息：
	```
	// 比如放在帧更新事件中，消息会作为队列加入，然后逐个处理。
	void Update()
	{
		MacroMessage.MainThreadProcessingMessage();
	}
	```

* 方法封装器
	提供一个方法软引用封装器，首先需要将一个可执行流程转换为<code>CallableTarget</code>对象。然后通过<code>AddCallableFunction</code>添加到全局字典中，随后可以通过<code>TryEvocation</code>或者<code>Evocation</code>调用。
	方法封装器默认可以搜索所有静态，实例，私有，公共的方法

----
#### 对象宏 (MacroObject)

在gameobject或transform上快速迭代的操作库。

* 场景迭代
	* 获取当前场景中的所有根对象：<code>GetActiveSceneRootGameObjects</code>。
	* 在所有已激活场景中查找组件：<code>GetAllSceneComponents</code>。
* 节点迭代
	* 获取父级迭代器：<code>GetParents</code>。
	* 获取子级迭代器：<code>GetChildren</code>。
	* 获取广度优先的子级迭代器：<code>GetChildrenBFS</code>。
	* 获取深度优先的子级迭代器：<code>GetChildrenRecursion</code>。(不要使用 GetChildrenDFS，存在严重性能缺陷)
* 组件迭代
	* 在当前位置无论如何都获取一个组件，如果没有就创建：<code>GetComponentAnyway</code>。
	* 在节点迭代器中找一个组件：<code>GetComponent</code>。
	* 将节点迭代器转为一一对应的组件迭代器：<code>GetComponentsOTO</code>。
	* 将节点迭代器转为组件迭代器：<code>GetComponentsOTOD</code>。
	* 在当前节点的所有子成员（包含子成员的子成员）内查找多个组件：<code>GetComponentsInRecursionChildren</code>。
	* 按照给定的组件顺序在节点迭代器中逐一查找：<code>GetFavoriteComponent</code>。
* 家族迭代
	* 获取当前节点的家族名称：<code>GetFamilyName</code>。
	* 使用家族名称搜索节点：<code>FindFamilyMember</code>。(支持一次重定向，以及逐级名称模糊查找)

以上仅包含大致用法，实际上包含更多的重载用法。

----
#### PID宏 (MacroMath)

提供一个PID计算器，具有基本的pid计算，同时带有基本安全计算。

----
#### 对象池宏

预设快速设置的对象池（CreatSimplePool）与顺序对象池（CreatObjectInOrderPool），传入预制体与父级，将直接返回一个预设的对象池。

提供一个联合对象池（预设的）<code>MacroPool.UnionSet</code>，支持开放预制体目标与父级目标参数到inspector。  
联合对象池需要手动调用Init函数进行初始化。

----
#### 矩形变换宏
#### 超驰宏 (MacroReflection)

* 签名转换：通过GetFieldsRename 获取给定名称的字段在自定义的重命名特性上的名称。
* 软接口：<code>SoftInterface</code> 通过名称直接设置或获取值，
	支持在一个对象上获取一系列名称的值，或在一系列对象上获取同一个名称的值，或是反过来设置他们。
* 实例化：通过<code>OverridingInstance</code>创建给定类的实例，支持通过全名
* 
----
#### 正则库 (MacroRegex)

提供了常用的正则匹配模式。

* GeneralVector：多维矢量文本匹配
* ObjMesh：obj模型参数匹配（匹配的同时会转换坐标系到unity）
* RegexReplace: 直接使用编辑器常用语法进行替换，比如"$1"获取第一个匹配项并输出。

----
#### 仓库宏
#### 材质宏
#### 平滑宏 （MacroSmooth）

包含28个常用插值函数，可以使用<code>SwitchEasing</code>对一个数值计算插值，返回重新映射插值位置的数值。  
插值函数可以单独调用，或使用枚举切换。   

----
#### 排序宏 (MacroSort)

c#自带的排序很好用，但是选择何种排序模式是根据数据类型自动切换的。  
在这里，排序宏中提供了一些可以手动选择的排序算法。   
注：没有猴子排序。  

----
#### 文本宏 (MacroMath)

提供：
* StringBuilderPool: 文本构建池
* join: 文本序列拼接。
	```
	List<object> objects = new List<object>();
	objects.Join(obj => obj.ToString(), )
	
	```
* 文本判断：空判断，空格判断，相等判断，字符集判断
* 格式包裹：支持TMP的26个富文本标签，或者自行扩展更多包裹器。  
	包裹器可以依靠多态模型来构建文本，而不依赖字面量构建。下面是一个示例：
	```
	_blockBuilder = new TextBlockBuilder(
		new ColorBlock(Color.white, "重量"),
		new DelegateBlock(() => value.ToString()), 
		new SizeBlock(20, "kg"))); 
	```
	TextBlockBuilder 是所有blocker的基类，使用它包裹所有文本将起到单纯拼接文本的作用；TextBlockBuilder 里的三个block的作用是使用相应的富文本标签包裹，根据标签的类型，包裹器会自动决定标签格式。  
	<code>DelegateBlock</code>的作用是通过委托来实时获取一个变量，示例中直接获取value的文本值，拼接到结果中。  
	TextBlockBuilder 可以不依靠如<code>DelegateBlock</code>这样的包裹器，可以直接将返回字符串类型的方法委托传入，blocker 会自动识别；前提是开启对应 blocker 里的<code>compatibility</code>兼容性检查，但这会带来额外的性能开销，如果没有特别的需求，建议还是使用<code>DelegateBlock</code>进行多态封装，这样也会更安全。

	在新的更新中，包裹器支持域写法，同时可以省略部分参数：
	```
	_blockBuilder = new TextBlockBuilder()
	{
		new ColorBlock() // 默认值就是白色
		{
			"重量"
		},
		new DelegateBlock(() => value.ToString()), 
		new SizeBlock(20) // 默认单位为像素
		{
			"kg"
		}
	}; 
	```
	这里的作用与上面的示例完全一致，更新的包裹器中支持自动填入更多的默认值了，比如颜色默认为白色。
	虽然看起来更长了，但是对于较复杂的格式来说可以有更强的可读性，建议这样写。

	同时，委托包裹器并不是唯一设置动态参数的方式:
	* <code>SetParamters</code>:在根包裹器上使用赋值方法，可以将一系列值一一对应地填入到后续一系列包裹器中。
	* <code>RefernceBlock</code>:反射包裹器允许传入一个对象，以及成员名称来访问属性（字段，访问器，方法均可）。
	* <code>ValueBlock</code>:值包裹器，搭配<code>SetParamters</code>来设置值，目标需要实现Tostring方法。
	* <code>NumberValueBlock</code>数值类型包裹器，搭配<code>SetParamters</code>来设置值，设置值会自动限制在最大最小范围内，如果给定的参数是字符串类型，将尝试通过默认的格式化方法转为数值。

	在实际使用时，只有<code>ValueBlock</code>默认会从<code>SetParamters</code>参数列表中获取值,
	其他包裹器需要手动通过<code>SetParameterization</code>参数化包裹器，否则不会响应参数列表。  

	值得时刻注意的是，<code>SetParamters</code>传递的值需要按深度优先的顺序逐一匹对，基本上可以认为是blockBuilder定义时的block先后顺序。  
	如果<code>SetParamters</code>中参数数量少于需求，在匹配时将会提前退出，导致后续值保持不变； 
	如果遇到强制类型检查的block且值无法匹配，block不会处理这个值，而是丢给下一个，只有遇到这种情况时，索引才有可能不是一一对应的形式。(默认认为是这么处理的，具体行为可以由block自己定义)

	关于性能：这种做法绝对不是省油的灯，绝对不要在运行时重复创建，而是声名在当前作用域的全局用作静态，或保持实例唯一。

* 文本转换：自动值转换为文本。
* NumberToChinese：将数值转为中文大写。
* 沃格纳费舍尔拼写检查器：检查一对或多对文本是否相似，返回最相似的文本序列，及相似度。
* ListToString：打印数组

#### 时间宏
#### 轴变换宏
#### 类型扩展
#### 矢量扩展

----
#### 导航宏

提供寻路算法核，寻路几何体构造器（未实装）。

* AStart寻路算法核   
通过<code>AStart2</code>提供，算法核只包含计算过程，需要被计算的对象上实现接口<code>IAstartNavNeighborNode</code>。  
接口中需要定义当前“可寻路对象”的距离模式，相邻代价，以及所有邻居。
如果需要定义管理器，可使用<code>AStartNode</code>进行快速封装。


----
#### 安全宏（计划废弃）

提供<code>ProgramFlowSecurity</code>类，以及相关的扩展和符属类型。  
安全宏用于，当一个方法中需要通过委托等方法动态调用其他一个或多个方法时，如果其中的某个方法报错，会导致后续所有方法不可用，且调用这些方法的方法也会因为错误抛出而停止，且如果运行在unity关键阶段，比如awake时，可能还会导致脚本被disable。  

使用安全宏封装要调用的委托，并指定调用时的标识位，当产生错误时，安全宏会捕获这个错误，并转为抛出错误信息，如果选择默认解决，那么这个错误将不会影响后续运行。  
但当下次调用时，安全宏将再次抛出一个错误信息，表示已经发生错误的委托被再次调用。  

----
#### 转换宏


----
#### 事件触发器宏

针对EventTrigger提供更快速的添加方法。

----
### Spline组件

提供<code>XericSplinePath</code>，<code>XericRectSplinePath</code>组件。
他们的区别是，一个用于三维世界，另一个用于UI界面。  
UI界面上的Spline组件会通过RectTransform控制，有区别与三维世界。  

<code>XericSplinePath</code>可以直接在unity中进行挂载，并通过界面编辑点，在列表中可以增加和减少点，或改变点的顺序。

在脚本中使用<code>XericSplinePath</code>定义字段，可以让脚本在inspecter上呈现一个引用端口，引用组件以获取曲线实例。

曲线通常用于静态计算，在设计上并未考虑动态刷新，动态构建曲线的方法，如果希望动态地构建曲线，可以自行根据其中的结构提供点位数据。    

在<code>XericSplinePath</code>曲线上初始时使用<code>BakePoint()</code>来烘焙一次数据，这个方法会在线组件上生成BakeData，有这个数据可以获得速度正确的的曲线映射，如果担心多个烘焙进行时的性能问题，可以使用协程方法<code>BakePointCoroutine()</code>推迟烘焙到来。  
烘焙的速度与精度有关，<code>BakePoint()</code>包含一个形参用于输入烘焙精度，默认100，表示进行100次采样，理论上采样越多，精度越高，但实际上并非越高越好，采样数量保持在数倍于实际点数量即可。  
烘焙后的曲线才可以使用重映射<code>RemappingTimeValue()</code>，同时也可以使用<code>SplineLength</code>获取曲线的长度。  

在曲线实例上可以访问<code>Evaluate...()</code>方法来计算坐标，旋转，数值，颜色过度。  
方法要求给定一个驱动值t，取值范围在0-1之间。  
默认情况下，驱动值是几乎等分地控制着每条线路的，无论两个点之间的距离长短，都近似为“时间”相等的多段线，这意味着无法通过这样的一个线驱动一个物体平滑地从起点运动到终点。  
而开头我们使用<code>BakePoint()</code>烘焙的数据就起作用了，通过调用线实例上的<code>RemappingTimeValue()</code>将时间驱动值转换为速度驱动值，这会带来非常平滑的效果。

2D的线路组件<code>XericRectSplinePath</code>工作在Rect上，如果屏幕的工作分辨率实际上是可变的，可能会导致实际线路有时会错位，这是因为线路工作的参考坐标系是静态的。  
如果当前项目的窗口缩放易变，建议每帧调用<code>UpdateRectDepend()</code>（一般都很难遇到分辨率亘古不变的情况），这个方法可以刷新曲线构建所依赖的矩形空间坐标。  

为了适配速度矫正后的线路，建议使用线路的长度作为循环的时间最大值，而不是强制所有线路从0-1取值，线路总长度可以使用<code>SplineLength</code>属性获取。    
这里是一个示例：（这个示例中使用的是二维曲线，但设置ui的方法是世界空间的，并非屏幕空间，这是因为我驱动的目标对象还是世界空间的Sprite，同时较小的ui坐标便于计算尾迹的长度，避免结果速度太快）
```
public void UpdateBySpline(XericRectSplinePath spline)
{
    totalTime = (float)spline.SplineLength;
    var remappingTime = time / totalTime; // 这是没有速度矫正的值
    remappingTime = spline.RemappingTimeValue(remappingTime); // 经过速度矫正
    // 设置坐标
    var pos = spline.EvaluatePosition(remappingTime); // 获得坐标
    transform.position = pos;
    // 设置旋转朝向运动方向
    var lastOffset = (lastPosition - pos).normalized;
    var ang = Mathf.Atan2(lastOffset.y, lastOffset.x) * Mathf.Rad2Deg;
    transform.localRotation = Quaternion.Euler(0, 0, ang);
    lastPosition = pos;
    // 设置缩放，来自曲线上定义的浮点值
    var size = spline.EvaluateValue(remappingTime);
	transform.localScale = size;
}
```

三维曲线与二维曲线类似，也可以通过烘焙操作来获得一些特殊方法的调用许可。

----
### 文件阅读器

提供<code>WWWDataDriver</code>和<code>WebFileLoader</code>网络文件读取功能。  

MacroFile库中对web平台的文件操作通过<code>WebFileLoader</code>实现，其中包含一个简单的协程文本加载。

<code>WWWDataDriver</code>则可以自行配置post或get请求，并允许将对象属性填入表单来进行请求。  
同时，允许下载文本，图片等内容。

----
### 简易批处理器

在代码中引用 XericLibrary.Runtime.Type.BatchProcess ，
并将需要进行批处理的对象实现 ICanBatchProcess<T> 接口，
其中提供了所有额外支持的生命周期事件，可以直接实现或显式实现。    

缺点是只有异步方法的生命周期约束，且没有提供线程安全的数据类型。 

和官方的相比没有什么优势，只是作为dll内部的部分程序的替代品，没事不要用这个。

----
### 多维布尔

提供<code>Bool1</code>,<code>Bool2</code>,<code>Bool3</code>,<code>Booln</code>。  
其中除了<code>Booln</code>是类，其他都是结构体，在相互转换时需要注意。

不同的多维布尔对象之间可以互相转换，且参数不会遗失。  
他们的区别是会在界面上绘制不同数量的参数。  

其中bool1与普通布尔几乎无二，可以直接隐式转为bool，只是他存储的方式为int，可以用于bool隐式转为int。

多维布尔可以与矢量运算：
* 乘法相当于使用多维布尔置零这个矢量对应轴。
* 除法时多维布尔作为被除数时，相当于对矢量各轴取倒数，并置零非选中轴。
* 除法时多维布尔作为除数时，相当于计算已被多维布尔置零对应轴的矢量的模长。

多维布尔可以与布尔运算：
* 异或操作可以对多维布尔各轴异或。

---- 
### 预设与函数退化链（废弃）

提供一个抽象基类<code>PremadeLinkedNode</code>，它以链表形态工作，通过<code>AddNode</code>添加另一个节点。  
添加完毕后使用<code>Invoke</code>并传入参数，这将自动根据参数形态选择函数。  

---- 
### 时间节点

提供<code>TimingStamp</code>,<code>TimeStamp</code>。

TimingStamp用于时间快速转换，以及在unity界面上呈现的能力。   
TimeStamp用于隐式的，在极短时间内（小于1tick）的时间戳的名称标记。

----
### 空对象

提供<code>Empty\<T\></code>用来快速创建一个空对象。  
这个空对象中包含空列表，等其他多种资源。  
值得注意的是，在创建空组件时（component）会从超驰库中使用自动实例化命令执行这一过程，可能会导致在场景中出现空节点（GameObject）。

----
### 类标签

提供<code>CanFind</code>系列特性。  
* 使用<code>CanFindClass</code>标记类   
	当使用被标记类的实例时，可以使用<code>.GetClassCanFindTag()</code>获取这个类上包含的所有标记。  
	或使用<code>.GetClassByCanFindTag()</code>获取这个标签被赋予的所有类。  
	配合MacroReflection库，可以直接在当前程序集中找到这个类的所有实例。  

* 使用<code>CanFindProperty</code>标记属性  
	

* 使用<code>Rename</code>标记属性  
	在editor中可以搜索特性来快速实现重命名，使用<code>TryGetMemberName</code>来获取FieldInfo名称，如果没有标记，那么使用属性名称，否则使用重命名。

* 使用<code>CusMe</code>标记类，属性，事件，委托等  
	需要手动通过不同的分类进行标记，如使用<code>CusMeProperty</code>来标记属性。  
	包含被标记的特性的类在创建实例时，可以通过<code>CusMeManagerment</code>的实例通过<code>AddTarget</code>添加目标，被添加的成员可以用于菜单构建。  

### 排挤网格 (代办)

提供<code>OstracizeGrid</code>排挤网格，网格各尺寸大小是由占据此网格的ui决定的；
当一个ui的大小发生改变后，占据更多网格，被占用网格的ui可以移动到周围空余的网格位置。

网格可以在世界空间中定义，或在屏幕空间定义，这会影响排挤网格的自动缩放
最终ui排挤通过屏幕空间计算。

----
---- 
## DataBaseControl （快捷数据库）

数据库控制器，当前主要工作区域是SqlClient部分

---- 
### 基础使用

数据库操作的指令被抽象到Order类了（注释里会称为解释器，指的就是order类），比如sqlClient就是sqlcOrder，其中sqlc代表sqlclient。  
order类本质上是一个文本拼接器，为了方便管理，你可以使用对应类的order.GetOrder来获取池中的一条指令，比如：
```
var orderValues = SQLCHelper.GetOrder();
```
这个orderValues必须手动释放才会回到池中，比如：
```
orderValues.Dispost();
```
GetOrder指令可以在编译时静态调用，用作静态参数，不去释放，这样的话指令的生命周期会跟随程序释放而释放。  
使用指令前建议先清空，避免指令污染：
```
orderValues.CleanOrder();
```
然后，可以在指令上调用对应的函数，里面预制了数据库连接增删改查，表增删改查，项增删改查，以及插入判断，筛选条件命令。  
对于连接功能，可以参考下面的示例：
``` 
public void InitDataBase(string hostName, string databaseName, string tableName,
	IEnumerable<MyData> valueData)
{
	SQLCHelper.connectOrder.OrderSetServer(hostName, null);
	// 数据库不存在，就创建一个
	if ((int)orderValues.OrderIsDataBaseExist(databaseName)
	.ExecuteScalar() <= 0)
		orderValues.OrderCreatDataBase(databaseName)
		.ExecuteNonQuery();

	SQLCHelper.connectOrder.OrderSetServer(hostName, databaseName);
	// 数据表不存在，就创建一个
	if ((int)orderValues.OrderIsTableExist(tableName)
	.ExecuteScalar() <= 0)
		orderValues.OrderCreatTable(tableName)
		.ExecuteNonQuery();
	
	// 构建数据项目
	orderValues.CleanValue();
	orderValues.Values.AddRange(valueData.Select(a => new SqlCOrder.SqlCValue(a.name, a.value)));

	// 数据项目不存在，就创建一个
	if (valueData != null)
	{
		orderValues.OrderInsetTableItem(tableName);
	}
}
```
可以看到前面会使用同一个order指令来执行命令构建，然后通过Execute方法来执行指令。  
指令在构建时会将文本压入stringBuilder中，直到遇到Execute指令后，指令才会被拼接成具体的文本，存入历史指令中。  
当这个指令中stringBuilder里没有待输出的指令时，就会使用之前拼接过的文本，也就是说一条已经构建的指令可以被重复执行。  

刚刚的示例代码也可以通过调用预制的扩展方法完成，比如：
```
orderValues.InitializationDataBase("localhost", "DEFAULT_DATABASE", "DEFAULT_TABLE");
```
上面这段指令会自动连接到地址localhost下，名为DEFAULT_DATABASE的DEFAULT_TABLE表中。  
如果数据库中没有库或表，将会自动创建。这对于仅仅为了保存数据的情况来说会比较方便，但它无法检查数据库中是否缺少键。  

这种扩展指令根据其使用情景可能会相对于普通的方法有所区别，就比方说刚刚这个扩展方法会立刻执行，不需要手动调用Execute方法。  
而一般指令都仅仅只是将命令填入拼接池中，然后在Execute中执行order里的SubmitOrder()来持久化指令内容。

如果希望在内存中保留一个与数据库对应的数值，则可以使用order的Values属性；  
一个order里的Values对应一个数据库表里的一列下的一个数据。  
Value的name对应列名，value对应实际值。  

还是上面那段完整的数据库创建函数实例中，最后的OrderInsetTableItem就是将values插入到表的指令，包括InitializationDataBase扩展中也会自动将当前执行的order里的values插入到表，所以这个指令会建议手动管理生命周期。  

这里说明一下，指令里的方法名称是有特定规律的：  
* 比如用于单段执行的指令方法，会使用Order开头，使用<code>.Order...</code>可以搜索到很多相关的指令。他们都会返回自身，用以支持链式的调用。
* 如果某个指令的后面可以继续添加指令，比如条件指令之类的，可以使用<code>.OrderJoin...</code>搜索到一些相关的预制指令。
* 而其他的指令则可以在扩展库中找到，扩展库中会专门针对特定的应用情景开放专门的方法。

----
#### 数据读写

在上面已经说过如何新建表，建表的时候会默认写入一次数据，而如果希望手动定义表格里的内容的话可以这么写：
```
order.OrderJoinValuesDefinitinOnly(orderValues.Values);
```
如果希望填入当前order里的Values的话，可以这么写：
```
```

读取可以使用：
```
orderValues.OrderSelect("DEFAULT_TABLE", null, null, true, true);
```
这段指令将orderValues里的value作为项目进行查询，两个null对应指定条件和排序，这里为空，所以这会返回所有value的值。
记得使用ExecuteReader执行，并返回阅读器进行处理，在阅读器中，需要手动根据值名称一一对应地将值填回orderValues里。  

不过在扩展类中也提供了自动地读取逻辑：
```
orderValues.ReadLatestValue("DEFAULT_TABLE");
```
这会读取数据库中的，orderValues中的值，并直接写在orderValues的每一项中。
然后直接对orderValues中的values进行处理姐可以了。

需要注意的是，这些指令虽然没有明确要求数据库连接，但还是至少需要连接过一次数据库，比如至少调用过一次InitializationDataBase方法，这样，后续的所有指令都可以不需要主动指定connectOrder项目。  
比如在最开始使用InitializationDataBase进行一次初始化即可。  



----
----
## Deconstruction （线路绘制，暂缓更新）

Deconstruction 部分由两个dll组成，一个是 Deconstruction(Runtime)，另一个是 Deconstruction Style(Editor)。

----
### 线路绘制

线路绘制组件支持二维，或三维的线条计算，线条继承自“可放置物”类，为了能够显示线条，需要向其提供渲染器和，同理，为了能够算出构成线的点集合，需要提供线计算器。  
为此插件内默认提供了一个使用lineRenderer组件的线渲染器，以及二维的直线和圆弧的线计算器。  
在计算平面上的CAD线路时，通过实现二维的线计算器，并实现它的坐标转换方法，即可在任意表面上实现线的绘制与保存。  

另外如果希望快速构建具有特制绘制规则的对象，则需要另外的可放置物管理器 PlaceElementManager ，
它继承于 SingleMonoBase ，所以可以通过单例的方式访问它。  

可放置物管理器管理的是所有的可放置物对象，和决策工具类 DecisionMakerToolBase ，以及它们的生命周期。  

工具类则提供了细致的针对如何放置对象的操作规则，并提供了操作栈表结构，
可以通过操作栈来实现具有链表关系的行为树，栈中的每个节点是行为树上的一个节点，
每个节点可以访问到上一个对象和下一个对象成员的节点。  

每个节点可以是正在绘制的线，正在计算寻路的ai，或其他任何行为，
在节点内可以直接通过 ReplaceThis(new Exit_OpSlot()); 方法类来切换行为树的节点到一个新的节点。  

* 基础规则  
建议在工具类中进行创建等操作，管理类中只进行管理操作，比如在插件的sesotho部分的非dll文件中，提供了SesothoPeManager和SesothoArrangementWiresTool，他们就是管理器与工具类的派生部分。

* 按键规则   
在没有任何工具启动的情况下，管理器可以接收按键输入，直到被工具输入抢占。
不过，管理器到工具中并没有任何抢占逻辑，所以请根据工具激活逻辑适时关闭按键处理。  
另外，在没有任何工具激活的情况下，工具本身的按键获取机制也不会激活，记得使用原生的按键获取逻辑。

* 线路规则


* 链路规则
链路是指实现了IConterminousness接口的对象，这个接口提供了一个获取ICoterminous接口的访问器；  
ICoterminous接口通常是用于实现链接关系的抽象层，简单的通过输入和输出来定义链接的线路，在接口中可以通过GetOpposite在当前对象的链接结构上下文中访问另一个与当前访问者相对应的链接对象，通常这是与访问者同类的对象，但是会经过接口进行抽象；而GetIdentical则可以获取与访问者同类链接的对象成员。

链路方向使用LinkType进行标记，也可以直接使用LinkType.SwapType()来快速反转这个链接标记；  
LinkType可以直接和LinkedPortType相互进行转换，也可以使用LinkType.ConvertType()来快速转换类型。  
链路方向通常只用在内部作为方向标识，外部无法获取，一般只在设置时进行一次标识设置即可。

----
### 线路结构

线路绘制组件提供了线路绘制，线路引用链控制等基本操作。

* <code>PromptLine2</code>提供近乎完善的线路计算与绘制操作，它继承于<code>PlacementBase</code>，这代表它是一个可放置对象。它实现了以下接口：
	* <code>IPossessorTrajectory2</code>：轨迹结构定义接口，接口要求线路实现渲染器和计算器组件，以及若干坐标、法线、长度计算结果获取的方法；
	* <code>IConterminousness</code>：链表相连结构接口，要求实现的对象上提供获取链路结构的属性，以及简单的加入和移除方法；
		* <code>ICoterminous</code>：一个允许枚举链路成员的对象，同时需要能够根据入口出口枚举方向来自定义迭代。
			* 这里可以使用<code>LinearPeriodicLink</code>用来表示线性连接数据结构，这个对象内只能定义入口和出口两个对象，可以使用入口或出口枚举来访问对应的成员。
			* 如果需要定义一组多个出入口对象集合的话，可以使用<code>GroupTheoreticLink</code>，然后通过入口出口枚举可以获取一组对应的成员。
* <code>PromptLine2</code>用于定义线路，这个类中包括吸附，相切计算在内的操作均在二维空间中计算，但默认所有的坐标均展示在三维世界的 untiy xz 平面上。  

----
### 生命周期


由于所有的成员都是通过批处理器完成生命周期的定义，批处理器的调用时机对于后续的流程来说至关重要。  
为了便于使用，所有的批处理对象的生命周期都遵循以下规则：

```
开始和结束时间在LateUpdate阶段触发；
主线程更新事件在Update阶段触发；
异步更新事件在LateUpdate阶段触发；

主线程事件在所有事件之后才执行；
```

* 安全检查
在批处理期间是允许销毁的，目前的安全限制仅限在更新前会检查一遍批处理的key值是否为空，在其他时刻，可以通过其他的接口传递的安全检查来辅助更新检查。  
比如在PromptLine2中，如果太早或太迟调用UpdateRender会导致在更新期间捕获到一个空引用的报错，来源是linerenderer组件在此时还不存在，具体原理不清楚，但是只会提示一次，所以此时的渲染操作可以直接跳过。

----
### 获取工具


如果希望获取放置物管理器中的工具：

```
// 获取当前激活的工具目标
DecisionMakerToolBase.ActiveDecisionMaker 
// 以及这个工具目标是否有效
DecisionMakerToolBase.HasAciveDecisionMaker
```

----
#### 按键处理

DecisionMakerToolBase中提供了预设的按键处理过程。

```
// 新建一个按键，并指定它要监听的键位
public static KeyPack Akey = new KeyPack(KeyCode.A);

// 获取状态，如果定义了多个，则所有按键按下才有效
Akey.Getkey();
// 获取包内任意按键按下时
Akey.GetKeyDown();
// 获取任意按键释放时
Akey.GetKeyUp();
// 获取包内任意按键按下或释放时
Akey.GetKeyDifferent();
// 获取包内任意按键按下
Akey.GetKeyCombinationDown();
```

这个KeyPack内允许传入多个按键值，用于简单判断组合键。  
比如通过Getkey获取状态时，所有按键都按下了才有效。
可以配合MacroKey中的符合逻辑判断更复杂的逻辑。

----
### 示例文件


在Runtime/HorizonLineOrbit中已经提供了基本的用例，将SesothoPeManager挂到任意一个节点上就可以了。  
运行后按下键盘L键，此时点击世界中layer为defualt的物体，就会点击处y轴为0的位置上生成一个线，移动光标，线也会跟着移动。  

这些脚本提供了一个基本的示例，并且几乎将所有可以进行配置修改的要素都呈现了出来，可以在后续的扩展中通过参考这些文件来进行实现。

在示例脚本中主要分为对元素的实现，以及对过程的实现；  
* terminalPoint, TerminalLine2T3, TerminalIland是后续创建的实例所挂载的具有确定空间，绘制方式的实现。  
* 其他的都是管理器，工具，操作栈的实现。  

其中管理器，工具，操作栈类都是层层嵌套的引用结构，尽量只让相邻的两个过程进行相互的引用，比如工具应该引用管理器的实现，开放给操作栈。


----
### 使用须知


现在已知的线路绘制问题:
* 如果同时创建大量的操作栈(>500),并在之后的某个时刻中将它们全部隐式地释放掉,有可能会在之后的某个时刻带来庞大的GC占用(>90ms)
* 逆时针绘制的曲线线路在第三象限可能没法计算最近点或计算错误。
