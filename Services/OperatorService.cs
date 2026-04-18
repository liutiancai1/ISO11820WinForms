using System;
using System.Linq;
using TestServer.Models;
using ISO11820WinForms.Models;

namespace ISO11820WinForms.Services
{
    public class OperatorService
    {
        /// <summary>
        /// 验证用户登录
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        /// <returns>用户对象，如果验证失败则返回null</returns>
        public Operator AuthenticateUser(string username, string password)
        {
            try
            {
                using (var context = new ISO11820DbContext())
                {
                    // 在实际应用中，密码应该是加密存储的
                    // 这里为了演示目的，直接比较明文密码
                    return context.Operators.FirstOrDefault(u => u.Username == username && u.Pwd == password);
                }
            }
            catch (Exception)
            {
                // 在实际应用中应该记录日志
                return null;
            }
        }

        /// <summary>
        /// 根据用户ID获取用户信息
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>用户对象</returns>
        public Operator GetOperatorById(string userId)
        {
            try
            {
                using (var context = new ISO11820DbContext())
                {
                    return context.Operators.FirstOrDefault(u => u.Userid == userId);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}