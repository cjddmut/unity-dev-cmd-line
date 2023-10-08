using UnityEngine;

namespace DevCmdLine.Test
{
    public static class TestCmds
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterSelfAssembly()
        {
            DevCmdManager.RegisterAssembly(typeof(TestCmds).Assembly);
        }



        [DevCmd(
            "test1",
            "",
            "name1", "name2", "quoted")]  
        [DevCmdComplete("name1", "val11", "val12")]
        [DevCmdComplete("name1", 1, "val21", "val22")]
        [DevCmdComplete("name2", "roger", "echo")]
        [DevCmdComplete("quoted", "two words", "three hella words")]
        [DevCmdComplete("quoted", 1, "q1", "q2")]
        [DevCmdComplete("quoted", 2, "deeper yea", "deeper no")]
        private static void TestCmd1(DevCmdArg[] args)
        {

        }
    }
}
