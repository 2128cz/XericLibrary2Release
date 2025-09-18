namespace Deconstruction.UI.Interface
{
    /// <summary>
    /// 层级控制
    /// </summary>
    public interface IHierarchyControl
    {
        /// <summary>
        /// 设置可见性 
        /// </summary>
        public bool GetActive();

        /// <summary>
        /// 设置可见性 
        /// </summary>
        public bool GetActiveInHierarchy();
        
        /// <summary>
        /// 设置可见性 
        /// </summary>
        public void SetActive(bool active);

        /// <summary>
        /// 设置可见性 
        /// </summary>
        public void SetActiveInHierarchy(bool active);
    }
}