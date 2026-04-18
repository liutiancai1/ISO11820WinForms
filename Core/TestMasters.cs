namespace ISO11820WinForms.Core
{
    public class TestMasters
    {
        //包含多台试验控制器的集合对象
        public Dictionary<int, TestMaster> DictTestMaster { get; set; }

        public TestMasters()
        {
            DictTestMaster = new Dictionary<int, TestMaster>();
        }

        public void addMaster(TestMaster master)
        {
            DictTestMaster.Add(DictTestMaster.Count, master);
        }
    }
}
