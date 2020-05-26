﻿using System.Net;
using Microsoft.AspNetCore.Mvc;
using SRSApis.SRSManager;
using SRSApis.SRSManager.Apis;
using SRSConfFile.SRSConfClass;
using SRSManageCommon;
using SRSWebApi.Attributes;

namespace SRSWebApi.Controllers
{
    /// <summary>
    /// vhosthttphooks接口类
    /// </summary>
    [ApiController]
    [Route("")]
    public class VhostHttpHooksController
    {
        /// <summary>
        /// 删除HttpHooks配置
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="vhostDomain"></param>
        /// <returns></returns>
        [HttpPost]
        [AuthVerify]
        [Log]
        [Route("/VhostHttpHooks/DeleteVhostHttpHooks")]
        public JsonResult DeleteVhostHttpHooks(string deviceId, string vhostDomain)
        {
            var rt = VhostHttpHooksApis.DeleteVhostHttpHooks(deviceId, vhostDomain, out ResponseStruct rs);
            return Program.CommonFunctions.DelApisResult(rt, rs);
        }

        /// <summary>
        /// 获取Vhost中的HttpHooks
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="vhostDomain"></param>
        /// <returns></returns>
        [HttpGet]
        [AuthVerify]
        [Log]
        [Route("/VhostHttpHooks/GetVhostHttpHooks")]
        public JsonResult GetVhostHttpHooks(string deviceId, string vhostDomain)
        {
            var rt = VhostHttpHooksApis.GetVhostHttpHooks(deviceId, vhostDomain, out ResponseStruct rs);
            return Program.CommonFunctions.DelApisResult(rt, rs);
        }

        /// <summary>
        /// 设置或创建HttpHooks
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="vhostDomain"></param>
        /// <param name="httpHooks"></param>
        /// <returns></returns>
        [HttpPost]
        [AuthVerify]
        [Log]
        [Route("/VhostHttpHooks/SetVhostHttpHooks")]
        public JsonResult SetVhostHttpHooks(string deviceId, string vhostDomain, HttpHooks httpHooks)
        {
            var rt = VhostHttpHooksApis.SetVhostHttpHooks(deviceId, vhostDomain, httpHooks, out ResponseStruct rs);
            return Program.CommonFunctions.DelApisResult(rt, rs);
        }

        
    }
}