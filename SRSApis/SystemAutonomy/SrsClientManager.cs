using System;
using System.Collections.Generic;
using System.Threading;
using SrsApis.SrsManager.Apis;
using SrsManageCommon;
using SRSManageCommon.DBMoudle;
using SRSManageCommon.ManageStructs;
using SrsManageCommon.SrsManageCommon;

namespace SRSApis.SystemAutonomy
{
    public class SrsClientManager
    {
        private int interval = SrsManageCommon.Common.SystemConfig.SrsClientManagerServiceinterval;

        private void rewriteMonitorType()
        {
            try
            {
                if (Common.SrsManagers != null)
                {
                    foreach (var srs in Common.SrsManagers)
                    {
                        if (srs == null || srs.Srs == null) continue;

                        if (srs.IsInit && srs.Srs != null && srs.IsRunning)
                        {
                            var onPublishList =
                                FastUsefulApis.GetOnPublishMonitorListByDeviceId(srs.SrsDeviceId,
                                    out ResponseStruct rs);
                            if (onPublishList == null || onPublishList.Count == 0) continue;
                            var ingestList = FastUsefulApis.GetAllIngestByDeviceId(srs.SrsDeviceId, out rs);

                            ushort? port = srs.Srs.Http_api!.Listen;
                            List<Channels> ret28181 = null!;
                            if (port != null && srs.Srs != null && srs.Srs.Http_api != null &&
                                srs.Srs.Http_api.Enabled == true)
                            {
                                ret28181 = GetGB28181Channels("http://127.0.0.1:" + port.ToString());
                            }

                            foreach (var client in onPublishList)
                            {
                                if (srs.Srs!.Http_api == null || srs.Srs.Http_api.Enabled == false) continue;

                                #region 处理28181设备

                                if (ret28181 != null)
                                {
                                    foreach (var r in ret28181)
                                    {
                                        if (!string.IsNullOrEmpty(r.Stream) && r.Stream.Equals(client.Stream))
                                        {
                                            lock (SrsManageCommon.Common.LockDbObjForOnlineClient)
                                            {
                                                var reti = OrmService.Db.Update<OnlineClient>()
                                                    .Set(x => x.MonitorType, MonitorType.GBT28181)
                                                    .Where(x => x.Client_Id == client.Client_Id)
                                                    .ExecuteAffrows();
                                            }
                                        }
                                    }
                                }

                                #endregion

                                #region 处理onvif设备

                                if (ingestList != null && ingestList.Count > 0)
                                {
                                    foreach (var ingest in ingestList)
                                    {
                                        if (ingest != null && ingest.Input != null
                                                           && client.RtspUrl != null &&
                                                           ingest.Input!.Url!.Equals(client.RtspUrl))
                                        {
                                            lock (SrsManageCommon.Common.LockDbObjForOnlineClient)
                                            {
                                                var reti = OrmService.Db.Update<OnlineClient>()
                                                    .Set(x => x.MonitorType, MonitorType.Onvif)
                                                    .Where(x => x.Client_Id == client.Client_Id)
                                                    .ExecuteAffrows();
                                            }
                                        }
                                    }
                                }

                                #endregion

                                #region 处理直播流

                                int retj = OrmService.Db.Update<OnlineClient>()
                                    .Set(x => x.MonitorType, MonitorType.Webcast)
                                    .Where(x => x.MonitorType == MonitorType.Unknow &&
                                                x.ClientType == ClientType.Monitor)
                                    .ExecuteAffrows();

                                #endregion
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.WriteLog("rewriteMonitorType异常", ex.Message + "\r\n" + ex.StackTrace, ConsoleColor.Yellow);
            }
        }

        private List<Channels> GetGB28181Channels(string httpUri)
        {
            string act = "/api/v1/gb28181?action=query_channel";
            string url = httpUri + act;
            try
            {
                string tmpStr = NetHelperNew.HttpGetRequest(url, null!);
                var ret = JsonHelper.FromJson<SrsT28181QueryChannelModule>(tmpStr);
                if (ret.Code == 0 && ret.Data != null)
                {
                    return ret.Data.Channels!;
                }

                return null!;
            }
            catch (Exception ex)
            {
                LogWriter.WriteLog("获取SRS-GB28181通道数据异常...", ex.Message + "\r\n" + ex.StackTrace, ConsoleColor.Yellow);
                return null!;
            }
        }

        private void completionOnvifIpAddress()
        {
            try
            {
                if (Common.SrsManagers != null)
                {
                    foreach (var srs in Common.SrsManagers)
                    {
                        if (srs == null || srs.Srs == null) continue;
                        if (srs.IsInit && srs.Srs != null && srs.IsRunning)
                        {
                            var ret = VhostIngestApis.GetVhostIngestNameList(srs.SrsDeviceId, out ResponseStruct rs);
                            if (ret != null)
                            {
                                foreach (var r in ret)
                                {
                                    var ingest = VhostIngestApis.GetVhostIngest(srs.SrsDeviceId, r.VhostDomain!,
                                        r.IngestInstanceName!,
                                        out rs);

                                    if (ingest != null)
                                    {
                                        string inputIp =
                                            SrsManageCommon.Common
                                                .GetIngestRtspMonitorUrlIpAddress(ingest.Input!.Url!)!;
                                        if (SrsManageCommon.Common.IsIpAddr(inputIp!))
                                        {
                                            lock (SrsManageCommon.Common.LockDbObjForOnlineClient)
                                            {
                                                var reti = OrmService.Db.Update<OnlineClient>()
                                                    .Set(x => x.MonitorIp, inputIp)
                                                    .Set(x => x.RtspUrl, ingest.Input!.Url!)
                                                    .Where(x => x.Stream!.Equals(ingest.IngestName) &&
                                                                x.Device_Id!.Equals(srs.SrsDeviceId) &&
                                                                (x.MonitorIp == null || x.MonitorIp == "" ||
                                                                 x.MonitorIp == "127.0.0.1"))
                                                    .ExecuteAffrows();
                                                if (reti > 0)
                                                {
                                                    LogWriter.WriteLog("补全Ingest拉流器中的摄像头IP地址...",
                                                        srs.SrsDeviceId + "/" + r.VhostDomain + "/" +
                                                        ingest.IngestName +
                                                        " 获取到IP:" + inputIp + " 获取到Rtsp地址:" + ingest.Input!.Url);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.WriteLog("completionOnvifIpAddress异常", ex.Message + "\r\n" + ex.StackTrace,
                    ConsoleColor.Yellow);
            }
        }

        private void completionT28181IpAddress()
        {
            try
            {
                if (Common.SrsManagers != null)
                {
                    foreach (var srs in Common.SrsManagers)
                    {
                        if (srs == null || srs.Srs == null) continue;
                        if (srs.IsInit && srs.Srs != null && srs.IsRunning)
                        {
                            ushort? port = srs.Srs.Http_api!.Listen;
                            if (port == null || srs.Srs.Http_api == null || srs.Srs.Http_api.Enabled == false)
                                continue;
                            var ret = GetGB28181Channels("http://127.0.0.1:" + port.ToString());
                            if (ret != null)
                            {
                                foreach (var r in ret)
                                {
                                    if (!string.IsNullOrEmpty(r.Rtp_Peer_Ip) && !string.IsNullOrEmpty(r.Stream))
                                    {
                                        lock (SrsManageCommon.Common.LockDbObjForOnlineClient)
                                        {
                                            var reti = OrmService.Db.Update<OnlineClient>()
                                                .Set(x => x.MonitorIp, r.Rtp_Peer_Ip)
                                                .Where(x => x.Stream!.Equals(r.Stream) &&
                                                            x.Device_Id!.Equals(srs.SrsDeviceId) &&
                                                            (x.MonitorIp == null || x.MonitorIp == "" ||
                                                             x.MonitorIp == "127.0.0.1"))
                                                .ExecuteAffrows();
                                            if (reti > 0)
                                            {
                                                LogWriter.WriteLog("补全StreamCaster收流器中的摄像头IP地址...",
                                                    srs.SrsDeviceId + "/" + r.Stream + " 获取到IP:" + r.Rtp_Peer_Ip);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.WriteLog("completionT28181IpAddress异常", ex.Message + "\r\n" + ex.StackTrace,
                    ConsoleColor.Yellow);
            }
        }

        private void clearOfflinePlayerUser()
        {
            try
            {
                if (Common.HaveAnySrsInstanceRunning())
                {
                    lock (SrsManageCommon.Common.LockDbObjForOnlineClient)
                    {
                        var re = OrmService.Db.Delete<OnlineClient>().Where(x => x.ClientType == ClientType.User &&
                                                                                 x.IsPlay == false &&
                                                                                 x.UpdateTime <=
                                                                                 DateTime.Now.AddMinutes(-3))
                            .ExecuteAffrows();
                        if (re > 0)
                        {
                            LogWriter.WriteLog("清理已经死亡的客户端播放连接...清理数量：" + re);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriter.WriteLog("clearOfflinePlayerUser", ex.Message + "\r\n" + ex.StackTrace, ConsoleColor.Yellow);
            }
        }

        private void Run()
        {
            while (true)
            {
                #region 补全ingest过来的monitorip地址

                completionOnvifIpAddress();

                Thread.Sleep(500);

                #endregion

                #region 补28181 monitorip 地址

                completionT28181IpAddress();

                Thread.Sleep(500);

                #endregion

                #region 删除长期没更新的user类型的非播放的客户端

                clearOfflinePlayerUser();

                Thread.Sleep(500);

                #endregion

                #region 重写摄像头类型

                rewriteMonitorType();

                Thread.Sleep(500);

                #endregion


                Thread.Sleep(interval);
            }
        }

        public SrsClientManager()
        {
            new Thread(new ThreadStart(delegate

            {
                try
                {
                    LogWriter.WriteLog("启动客户端监控服务...(循环间隔：" + interval + "ms)");
                    Run();
                }
                catch (Exception ex)
                {
                    LogWriter.WriteLog("启动客户端监控服务失败...", ex.Message + "\r\n" + ex.StackTrace, ConsoleColor.Yellow);
                }
            })).Start();
        }
    }
}