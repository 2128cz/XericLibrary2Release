<h2 align="center">Deconstruction</h2>

<a href="https://gitee.com/none_9_0/xeric-library2"> 发布页面</a>

## 简介

unity通用库插件，现已转为unity 6000版本。

此插件为XericDigitalTwinTool插件库的前置依赖项。与MateriaLibrary材质库一起使用更好哦！

单独使用可能会遇到编译报错，可以直接删除报错指向的文件。造成报错的原因可能是版本不兼容，依赖项uid错误等等。如果能力足够，请自行解决。

## 软件架构

* CentralizeLog: 小日志，unity本身的日志在调用时开销巨大，所以提供一个小日志的类型保存临时的日志内容。
* Deconstruction: 通过数学公式对线进行描述，进行标准的CAD线路参数计算，并支持在线路上进行导航，合理性计算等
* RescissionRework: 撤销重做 🕑
* SerializerHelper: 序列化库
* XericLibrary: 实用性unity宏库，包含一些实用扩展用法。

## Deconstruction （线路绘制，暂缓更新）

Deconstruction 部分由两个dll组成，一个是 Deconstruction，另一个是 Deconstruction Style 。

### 线路绘制
----


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



### 线路结构
----

线路绘制组件提供了线路连接引用关系，可以将一个线连接到另一个线上，也可以用点将它们连接起来，
默认线


### 生命周期
----


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

### 获取工具
----


如果希望获取放置物管理器中的工具：

```
// 获取当前激活的工具目标
DecisionMakerToolBase.ActiveDecisionMaker 
// 以及这个工具目标是否有效
DecisionMakerToolBase.HasAciveDecisionMaker
```

### 示例文件
----


在Runtime/HorizonLineOrbit中已经提供了基本的用例，将SesothoPeManager挂到任意一个节点上就可以了。  
运行后按下键盘L键，此时点击世界中layer为defualt的物体，就会点击处y轴为0的位置上生成一个线，移动光标，线也会跟着移动。  

这些脚本提供了一个基本的示例，并且几乎将所有可以进行配置修改的要素都呈现了出来，可以在后续的扩展中通过参考这些文件来进行实现。

在示例脚本中主要分为对元素的实现，以及对过程的实现；  
* terminalPoint, TerminalLine2T3, TerminalIland是后续创建的实例所挂载的具有确定空间，绘制方式的实现。  
* 其他的都是管理器，工具，操作栈的实现。  

其中管理器，工具，操作栈类都是层层嵌套的引用结构，尽量只让相邻的两个过程进行相互的引用，比如工具应该引用管理器的实现，开放给操作栈。


### 使用须知
----


现在已知的线路绘制问题:
* 如果同时创建大量的操作栈(>500),并在之后的某个时刻中将它们全部隐式地释放掉,有可能会在之后的某个时刻带来庞大的GC占用(>90ms)
* 逆时针绘制的曲线线路没法计算最近点。

## XericLibrary （主要核心库）

XericLibrary 部分由两个dll组成，一个是 Xeric Library，另一个是 Xeric Library Editor 。

### 宏库

#### 特性库
----

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
#### 颜色宏
#### 常数宏
#### 曲线宏
#### 枚举宏

枚举宏包含一个缓存封装器，可以通过<code>MacroEnum\<enum\></code>来定义。  
定义枚举结构封装将允许像列表一样访问这个枚举，或将枚举实例赋予，通过枚举封装器来操作枚举值。  
在新版中，枚举封装器可以使用单例写法访问，而不再需要手动声名保存实例了。  

#### 方程宏
#### 事件宏
#### 文件宏
#### 步进宏
#### 按键宏
#### 集合宏
#### 数学宏
#### 消息库 

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


#### 对象宏
#### PID宏
#### 对象池宏

预设快速设置的对象池（CreatSimplePool）与顺序对象池（CreatObjectInOrderPool），传入预制体与父级，将直接返回一个预设的对象池。

提供一个联合对象池（预设的）<code>MacroPool.UnionSet</code>，支持开放预制体目标与父级目标参数到inspector。  
联合对象池需要手动调用Init函数进行初始化。

#### 矩形变换宏
#### 超驰宏
#### 仓库宏
#### 材质宏
#### 平滑宏
#### 排序宏

c#自带的排序很好用，但是选择何种排序模式是根据数据类型自动切换的。  
在这里，排序宏中提供了一些可以手动选择的排序算法。   
注：没有猴子排序。  

#### 文本宏

提供：
* StringBuilderPool: 文本构建池
* join: 文本序列拼接。
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

	在新的更新中，包裹器支持大括号写法，同时可以省略部分参数：
	```
	_blockBuilder = new TextBlockBuilder()
	{
		new ColorBlock()
		{
			"重量"
		},
		new DelegateBlock(() => value.ToString()), 
		new SizeBlock(20)
		{
			"kg"
		}
	}; 
	```
	这里的作用与上面的示例完全一致，更新的包裹器中支持自动填入更多的默认值了，比如颜色默认为白色。

* 文本转换：自动值转换为文本。
* NumberToChinese：将数值转为中文大写。
* 沃格纳费舍尔拼写检查器：检查一对或多对文本是否相似，返回最相似的文本序列，及相似度。

#### 时间宏
#### 轴变换宏
#### 类型扩展
#### 矢量扩展

#### 导航宏
----

提供寻路算法核，寻路几何体构造器（未实装）。

* AStart寻路算法核   
通过<code>AStart2</code>提供，算法核只包含计算过程，需要被计算的对象上实现接口<code>IAstartNavNeighborNode</code>。  
接口中需要定义当前“可寻路对象”的距离模式，相邻代价，以及所有邻居。
如果需要定义管理器，可使用<code>AStartNode</code>进行快速封装。


#### 安全宏（计划废弃）
----

提供<code>ProgramFlowSecurity</code>类，以及相关的扩展和符属类型。  
安全宏用于，当一个方法中需要通过委托等方法动态调用其他一个或多个方法时，如果其中的某个方法报错，会导致后续所有方法不可用，且调用这些方法的方法也会因为错误抛出而停止，且如果运行在unity关键阶段，比如awake时，可能还会导致脚本被disable。  

使用安全宏封装要调用的委托，并指定调用时的标识位，当产生错误时，安全宏会捕获这个错误，并转为抛出错误信息，如果选择默认解决，那么这个错误将不会影响后续运行。  
但当下次调用时，安全宏将再次抛出一个错误信息，表示已经发生错误的委托被再次调用。  

#### 转换宏
----


### Spline组件
----

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


### 简易批处理器
----

在代码中引用 XericLibrary.Runtime.Type.BatchProcess ，
并将需要进行批处理的对象实现 ICanBatchProcess<T> 接口，
其中提供了所有额外支持的生命周期事件，可以直接实现或显式实现。    

缺点是只有异步方法的生命周期约束，且没有提供线程安全的数据类型。 

和官方的相比没有什么优势，只是作为dll内部的部分程序的替代品，没事不要用这个。

### 多维布尔
----

提供<code>Bool1</code>,<code>Bool2</code>,<code>Bool3</code>,<code>Booln</code>。  
其中除了<code>Booln</code>是类，其他都是结构体，在相互转换时需要注意。

不同的多维布尔对象之间可以互相转换，且参数不会遗失。  
他们的区别是会在界面上绘制不同数量的参数。  

其中bool1与普通布尔几乎无二，可以直接隐式转为bool，只是他存储的方式为int，可以用于bool隐式转为int。

### 时间节点
---- 
提供<code>TimingStamp</code>,<code>TimeStamp</code>。

TimingStamp用于时间快速转换，以及在unity界面上呈现的能力。   
TimeStamp用于隐式的，在极短时间内（小于1tick）的时间戳的名称标记。

## DataBaseControl （快捷数据库）

数据库控制器，当前主要工作区域是SqlClient部分

---- 

### 基础使用

数据库操作的指令被抽象到Order类了（注释里会称为解释器），比如sqlClient就是sqlcOrder，其中sqlc代表sqlclient。  
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
当这个指令中stringBuilder里没有待输出的指令时，会优先使用之前拼接过的文本，也就是说一条已经构建的指令可以被重复执行。  

刚刚的示例代码也可以通过调用预制的扩展方法完成，比如：
```
orderValues.InitializationDataBase("localhost", "DEFAULT_DATABASE", "DEFAULT_TABLE");
```
上面这段指令会自动连接到地址localhost下，名为DEFAULT_DATABASE的DEFAULT_TABLE表中。  
如果数据库中没有库或表，将会自动创建。这对于仅仅为了保存数据的情况来说会比较方便。  
这种扩展指令根据其使用情景可能会相对于普通的方法有所区别，就比方说这个扩展方法会立刻执行，不需要手动调用Execute方法。  
一般指令都仅仅只是将命令填入拼接池中，然后在Execute中执行order里的SubmitOrder()来持久化指令内容。

如果希望在内存中保留一个与数据库对应的数值，则可以使用order的Values属性；  
一个order里的Values对应一个数据库表里的一列下的一个数据。  
Value的name对应列名，value对应实际值。  

还是上面那段完整的数据库创建函数实例中，最后的OrderInsetTableItem就是将values插入到表的指令，包括InitializationDataBase扩展中也会自动将当前执行的order里的values插入到表，所以这个指令会建议手动管理生命周期。  

这里说明一下，指令里的方法名称是有特定规律的：  
* 比如用于单段执行的指令方法，会使用Order开头，使用<code>.Order...</code>可以搜索到很多相关的指令。他们都会返回自身，用以支持链式的调用。
* 如果某个指令的后面可以继续添加指令，比如条件指令之类的，可以使用<code>.OrderJoin...</code>搜索到一些相关的预制指令。
* 而其他的指令则可以在扩展库中找到，扩展库中会专门针对特定的应用情景开放专门的方法。

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
这段指令将orderValues里的value作为项目进行查询，由于两个null没有指定条件和排序，所以这会返回所有value的值。
记得使用ExecuteReader执行，并返回阅读器进行处理，在阅读器中，需要手动根据值名称一一对应地将值填回orderValues里。  

不过在扩展类中也提供了自动地读取逻辑：
```
orderValues.ReadLatestValue("DEFAULT_TABLE");
```
这会读取数据库中的，orderValues中的值，并直接写在orderValues的每一项中。
然后直接对orderValues中的values进行处理姐可以了。

需要注意的是，这些指令虽然没有明确要求数据库连接，但还是至少需要连接过一次数据库，比如至少调用过一次InitializationDataBase方法，这样，后续的所有指令都可以不需要主动指定connectOrder项目。


