using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using Cysharp.Threading.Tasks;
using System;
namespace NuoYan.Archive
{
    /// <summary>
    /// 实现此接口以自定义存档数据的子文件夹路径
    /// </summary>
    public interface IArhivePathPar
    {
        string DataPath { get; set; }
    }
    [DefaultExecutionOrder(-800)]
    public class ArchiveSystem<T1, U> : MonoBehaviour where T1 : ArchiveDataBase, new() where U : ArchiveTableBase, new()
    {
        private static ArchiveSystem<T1, U> _instance;
        public static ArchiveSystem<T1, U> Instance => _instance;
        private ArchiveSetting m_ArchiveSetting => ArchiveSetting.Instance;
        private readonly List<IArchive> m_ArchiveList = new List<IArchive>();
        [SerializeField]
        private List<U> m_ArchiveTableList = new List<U>();
        [SerializeField]
        private List<T1> m_ArchiveDataList = new List<T1>();
        [SerializeField]
        private T1 m_CurrentArchiveData = new T1();
        public T1 CurrentArchiveData => m_CurrentArchiveData;
        private string m_MainFolderPath;

        /// <summary>
        /// 错误事件
        /// </summary>
        public event EventHandler<ArchiveErrorEventArgs> OnError;

        /// <summary>
        /// 触发错误事件
        /// </summary>
        /// <param name="errorType">错误类型</param>
        /// <param name="errorMessage">错误消息</param>
        /// <param name="exception">异常对象</param>
        protected void RaiseError(ArchiveErrorType errorType, string errorMessage, Exception exception = null)
        {
            OnError?.Invoke(this, new ArchiveErrorEventArgs
            {
                ErrorType = errorType,
                ErrorMessage = errorMessage,
                Exception = exception
            });
            Debug.LogError($"存档系统错误 ({errorType}): {errorMessage}");
            if (exception != null)
            {
                Debug.LogError(exception);
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
            if (ArchiveSystemHelper.TryCreateMainFolderFolder(m_ArchiveSetting, out m_MainFolderPath))
            {
                Debug.Log($"存档主文件夹路径: {m_MainFolderPath}");
            }
            else
            {
                Debug.LogError("创建存档主文件夹失败");
            }
        }
        public void Register(IArchive data)
        {
            if (!m_ArchiveList.Contains(data))
            {
                m_ArchiveList.Add(data);
                data.GetData(m_CurrentArchiveData);
            }
        }
        public void Unregister(IArchive data)
        {
            if (m_ArchiveList.Contains(data))
            {
                m_ArchiveList.Remove(data);
                data.SetData(m_CurrentArchiveData);
            }
        }
        /// <summary>
        /// 保存存档
        /// </summary>
        public static void Save()
        {
            switch (Instance.m_ArchiveSetting.ArchiveType)
            {
                case ArchiveType.Single:
                    Instance.SaveSingle();
                    break;
                case ArchiveType.Multiple:
                    Instance.SaveMultiple();
                    break;
            }
        }
        /// <summary>
        /// 异步保存存档
        /// </summary>
        /// <returns></returns>
        public static UniTask SaveAsync()
        {
            switch (Instance.m_ArchiveSetting.ArchiveType)
            {
                case ArchiveType.Single:
                    return Instance.SaveSingleAsync();
                case ArchiveType.Multiple:
                    return Instance.SaveMultipleAsync();
                default:
                    return UniTask.CompletedTask;
            }
        }
        /// <summary>
        /// 加载存档
        /// </summary>
        public static void Load()
        {
            switch (Instance.m_ArchiveSetting.ArchiveType)
            {
                case ArchiveType.Single:
                    Instance.LoadSingle();
                    break;
                case ArchiveType.Multiple:
                    Instance.LoadMultiple();
                    break;
            }
        }
        /// <summary>
        /// 异步加载存档
        /// </summary>
        /// <returns></returns>
        public static UniTask LoadAsync()
        {
            switch (Instance.m_ArchiveSetting.ArchiveType)
            {
                case ArchiveType.Single:
                    return Instance.LoadSingleAsync();
                case ArchiveType.Multiple:
                    return Instance.LoadMultipleAsync();
                default:
                    return UniTask.CompletedTask;
            }
        }
        public static void Load(long timestamp)
        {
            try
            {
                string childFolder = ArchiveSystemHelper.GetChildFolder(Instance.m_MainFolderPath, Instance.m_CurrentArchiveData.GetType());
                if (string.IsNullOrEmpty(childFolder))
                {
                    Debug.LogError("获取子文件夹失败，存档加载失败");
                    return;
                }
                string filePath = Path.Combine(childFolder, $"{timestamp}.json");
                if (File.Exists(filePath))
                {
                    using (StreamReader sr = new StreamReader(filePath))
                    {
                        string json = sr.ReadToEnd();
                        if (Instance.m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                        {
                            json = Rijindael.Decrypt(json, Instance.m_ArchiveSetting.KEY, Instance.m_ArchiveSetting.IV);
                        }
                        Instance.m_CurrentArchiveData = JsonConvert.DeserializeObject<T1>(json);
                        foreach (var data in Instance.m_ArchiveList)
                        {
                            data.SetData(Instance.m_CurrentArchiveData);
                        }
                    }
                }
                else
                {
                    Debug.LogError($"存档文件不存在: {filePath}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"读取存档失败: {ex.Message}");
            }
        }
        public static UniTask LoadAsync(long timestamp)
        {
            return UniTask.RunOnThreadPool(() =>
            {
                Load(timestamp);
            });
        }
        private void SaveSingle()
        {
            try
            {
                foreach (var data in m_ArchiveList)
                {
                    data.GetData(m_CurrentArchiveData);
                }
                string json = JsonConvert.SerializeObject(m_CurrentArchiveData);
                if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                {
                    json = Rijindael.Encrypt(json, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                }
                string childFolder = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData.GetType());
                if (string.IsNullOrEmpty(childFolder))
                {
                    RaiseError(ArchiveErrorType.FolderCreationFailed, "获取子文件夹失败，存档保存失败");
                    return;
                }
                string filePath = Path.Combine(childFolder, m_ArchiveSetting.FileName);
                //存档存在则覆盖
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                using (StreamWriter sw = new StreamWriter(filePath, false))
                {
                    sw.Write(json);
                }
            }
            catch (System.Exception ex)
            {
                RaiseError(ArchiveErrorType.SaveFailed, $"存档失败: {ex.Message}", ex);
            }
        }
        private async UniTask SaveSingleAsync()
        {
            await UniTask.RunOnThreadPool(async () =>
            {
                try
                {
                    foreach (var data in m_ArchiveList)
                    {
                        data.GetData(m_CurrentArchiveData);
                    }
                    string json = JsonConvert.SerializeObject(m_CurrentArchiveData);
                    if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                    {
                        json = Rijindael.Encrypt(json, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                    }
                    string childFolder = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData.GetType());
                    if (string.IsNullOrEmpty(childFolder))
                    {
                        RaiseError(ArchiveErrorType.FolderCreationFailed, "获取子文件夹失败，存档保存失败");
                        return;
                    }
                    string filePath = Path.Combine(childFolder, m_ArchiveSetting.FileName);
                    //存档存在则覆盖
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    using (StreamWriter sw = new StreamWriter(filePath, false))
                    {
                        await sw.WriteAsync(json);
                    }
                }
                catch (System.Exception ex)
                {
                    RaiseError(ArchiveErrorType.SaveFailed, $"存档失败: {ex.Message}", ex);
                }
            });
        }
        private void LoadSingle()
        {
            try
            {
                string childFolder = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData.GetType());
                if (string.IsNullOrEmpty(childFolder))
                {
                    RaiseError(ArchiveErrorType.FolderCreationFailed, "获取子文件夹失败，存档加载失败");
                    return;
                }
                string filePath = Path.Combine(childFolder, m_ArchiveSetting.FileName);
                if (File.Exists(filePath))
                {
                    using (StreamReader sr = new StreamReader(filePath))
                    {
                        string json = sr.ReadToEnd();
                        if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                        {
                            json = Rijindael.Decrypt(json, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                        }
                        m_CurrentArchiveData = JsonConvert.DeserializeObject<T1>(json);
                        foreach (var data in m_ArchiveList)
                        {
                            data.SetData(m_CurrentArchiveData);
                        }
                    }
                }
                else
                {
                    SaveSingle();
                    RaiseError(ArchiveErrorType.FileNotFound, $"存档文件不存在: {filePath}");
                }
            }
            catch (System.Exception ex)
            {
                RaiseError(ArchiveErrorType.LoadFailed, $"读取存档失败: {ex.Message}", ex);
            }
        }
        private UniTask LoadSingleAsync()
        {
            return UniTask.RunOnThreadPool(() =>
            {
                LoadSingle();
            });
        }
        private void SaveMultiple()
        {
            try
            {
                U archiveTable = new U();
                m_CurrentArchiveData = new T1();
                foreach (var data in m_ArchiveList)
                {
                    data.GetData(m_CurrentArchiveData);
                }
                m_CurrentArchiveData.TimeStamp = archiveTable.TimeStamp;
                // 检查是否已存在相同时间戳的存档
                int existingIndex = m_ArchiveTableList.FindIndex(t => t.TimeStamp == archiveTable.TimeStamp);
                if (existingIndex >= 0)
                {
                    // 更新现有存档
                    m_ArchiveTableList[existingIndex] = archiveTable;
                    m_ArchiveDataList[existingIndex] = m_CurrentArchiveData;
                }
                else
                {
                    // 添加新存档
                    m_ArchiveTableList.Add(archiveTable);
                    m_ArchiveDataList.Add(m_CurrentArchiveData);
                }
                MarkCurrentLoaded(m_CurrentArchiveData.TimeStamp);
                string json1 = JsonConvert.SerializeObject(m_ArchiveTableList);

                using (StreamWriter sw = new StreamWriter(Path.Combine(m_MainFolderPath, ArchiveSetting.ARCHIVETABLE), false))
                {
                    sw.Write(json1);
                }
                // 只保存当前新创建的存档
                string json2 = JsonConvert.SerializeObject(m_CurrentArchiveData);
                if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                {
                    json2 = Rijindael.Encrypt(json2, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                }
                string childFolder = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData.GetType());
                if (string.IsNullOrEmpty(childFolder))
                {
                    Debug.LogError("获取子文件夹失败，存档保存失败");
                    return;
                }
                string filePath = Path.Combine(childFolder, $"{archiveTable.TimeStamp}.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                using (StreamWriter sw = new StreamWriter(filePath, false))
                {
                    sw.Write(json2);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"存档失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 覆写存档，传入时间戳参数以指定存档文件名，适用于多存档模式
        /// </summary>
        /// <param name="timeStamp"></param> <summary>
        public void Save(long timeStamp)
        {
            try
            {
                U archiveTable = new U()
                {
                    TimeStamp = timeStamp,
                };
                m_CurrentArchiveData = new T1();
                foreach (var data in m_ArchiveList)
                {
                    data.GetData(m_CurrentArchiveData);
                }
                m_CurrentArchiveData.TimeStamp = archiveTable.TimeStamp;
                // 检查是否已存在相同时间戳的存档
                int existingIndex = m_ArchiveTableList.FindIndex(t => t.TimeStamp == archiveTable.TimeStamp);
                if (existingIndex > 0)
                {
                    // 更新现有存档
                    m_ArchiveTableList[existingIndex] = archiveTable;
                    m_ArchiveDataList[existingIndex] = m_CurrentArchiveData;
                }
                else
                {
                    // 添加新存档
                    m_ArchiveTableList.Add(archiveTable);
                    m_ArchiveDataList.Add(m_CurrentArchiveData);
                }
                MarkCurrentLoaded(m_CurrentArchiveData.TimeStamp);
                string json1 = JsonConvert.SerializeObject(m_ArchiveTableList);

                using (StreamWriter sw = new StreamWriter(Path.Combine(m_MainFolderPath, ArchiveSetting.ARCHIVETABLE), false))
                {
                    sw.Write(json1);
                }
                // 只保存当前新创建的存档
                string json2 = JsonConvert.SerializeObject(m_CurrentArchiveData);
                if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                {
                    json2 = Rijindael.Encrypt(json2, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                }

                string childFolder = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData.GetType());
                if (string.IsNullOrEmpty(childFolder))
                {
                    Debug.LogError("获取子文件夹失败，存档保存失败");
                    return;
                }
                string filePath = Path.Combine(childFolder, $"{archiveTable.TimeStamp}.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                using (StreamWriter sw = new StreamWriter(filePath, false))
                {
                    sw.Write(json2);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"存档失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 异步保存存档，传入时间戳参数以指定存档文件名，适用于多存档模式
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <returns></returns>
        public async UniTask SaveAsync(long timeStamp)
        {
            await UniTask.RunOnThreadPool(async () =>
            {
                try
                {
                    U archiveTable = new U()
                    {
                        TimeStamp = timeStamp,
                    };
                    m_CurrentArchiveData = new T1();
                    foreach (var data in m_ArchiveList)
                    {
                        data.GetData(m_CurrentArchiveData);
                    }
                    m_CurrentArchiveData.TimeStamp = archiveTable.TimeStamp;
                    // 检查是否已存在相同时间戳的存档
                    int existingIndex = m_ArchiveTableList.FindIndex(t => t.TimeStamp == archiveTable.TimeStamp);
                    if (existingIndex >= 0)
                    {
                        // 更新现有存档
                        m_ArchiveTableList[existingIndex] = archiveTable;
                        m_ArchiveDataList[existingIndex] = m_CurrentArchiveData;
                    }
                    else
                    {
                        // 添加新存档
                        m_ArchiveTableList.Add(archiveTable);
                        m_ArchiveDataList.Add(m_CurrentArchiveData);
                    }
                    MarkCurrentLoaded(m_CurrentArchiveData.TimeStamp);
                    string json1 = JsonConvert.SerializeObject(m_ArchiveTableList);

                    using (StreamWriter sw = new StreamWriter(Path.Combine(m_MainFolderPath, ArchiveSetting.ARCHIVETABLE), false))
                    {
                        await sw.WriteAsync(json1);
                    }
                    // 只保存当前新创建的存档
                    string json2 = JsonConvert.SerializeObject(m_CurrentArchiveData);
                    if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                    {
                        json2 = Rijindael.Encrypt(json2, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                    }
                    string childFolder = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData.GetType());
                    string filePath = Path.Combine(childFolder, $"{archiveTable.TimeStamp}.json");
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    using (StreamWriter sw = new StreamWriter(filePath, false))
                    {
                        await sw.WriteAsync(json2);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"存档失败: {ex.Message}");
                }
            });
        }
        private async UniTask SaveMultipleAsync()
        {
            await UniTask.RunOnThreadPool(async () =>
            {
                try
                {
                    U archiveTable = new U();
                    m_CurrentArchiveData = new T1();
                    foreach (var data in m_ArchiveList)
                    {
                        data.GetData(m_CurrentArchiveData);
                    }
                    m_CurrentArchiveData.TimeStamp = archiveTable.TimeStamp;
                    // 检查是否已存在相同时间戳的存档
                    int existingIndex = m_ArchiveTableList.FindIndex(t => t.TimeStamp == archiveTable.TimeStamp);
                    if (existingIndex >= 0)
                    {
                        // 更新现有存档
                        m_ArchiveTableList[existingIndex] = archiveTable;
                        m_ArchiveDataList[existingIndex] = m_CurrentArchiveData;
                    }
                    else
                    {
                        // 添加新存档
                        m_ArchiveTableList.Add(archiveTable);
                        m_ArchiveDataList.Add(m_CurrentArchiveData);
                    }
                    MarkCurrentLoaded(m_CurrentArchiveData.TimeStamp);
                    string json1 = JsonConvert.SerializeObject(m_ArchiveTableList);

                    using (StreamWriter sw = new StreamWriter(Path.Combine(m_MainFolderPath, ArchiveSetting.ARCHIVETABLE), false))
                    {
                        await sw.WriteAsync(json1);
                    }
                    // 只保存当前新创建的存档
                    string json2 = JsonConvert.SerializeObject(m_CurrentArchiveData);
                    if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                    {
                        json2 = Rijindael.Encrypt(json2, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                    }
                    string childFolder = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData.GetType());
                    if (string.IsNullOrEmpty(childFolder))
                    {
                        Debug.LogError("获取子文件夹失败，存档保存失败");
                        return;
                    }
                    string filePath = Path.Combine(childFolder, $"{archiveTable.TimeStamp}.json");
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    using (StreamWriter sw = new StreamWriter(filePath, false))
                    {
                        await sw.WriteAsync(json2);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"存档失败: {ex.Message}");
                }
            });
        }
        private void LoadMultiple()
        {
            try
            {
                string archiveTablePath = Path.Combine(m_MainFolderPath, ArchiveSetting.ARCHIVETABLE);
                if (File.Exists(archiveTablePath))
                {
                    using (StreamReader sr = new StreamReader(archiveTablePath))
                    {
                        string json = sr.ReadToEnd();
                        m_ArchiveTableList = JsonConvert.DeserializeObject<List<U>>(json);
                    }
                    m_ArchiveDataList.Clear();
                    string childFolder = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData.GetType());
                    if (string.IsNullOrEmpty(childFolder))
                    {
                        Debug.LogError("获取子文件夹失败，存档加载失败");
                        return;
                    }
                    foreach (var file in m_ArchiveTableList)
                    {
                        string archiveDataPath = Path.Combine(childFolder, $"{file.TimeStamp}.json");
                        if (File.Exists(archiveDataPath))
                        {
                            using (StreamReader sr2 = new StreamReader(archiveDataPath))
                            {
                                string json2 = sr2.ReadToEnd();
                                if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                                {
                                    json2 = Rijindael.Decrypt(json2, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                                }
                                m_ArchiveDataList.Add(JsonConvert.DeserializeObject<T1>(json2));
                            }
                        }
                    }
                    if (m_ArchiveDataList.Count > 0)
                    {
                        if (!HasMarkedLoaded())
                        {
                            m_CurrentArchiveData = m_ArchiveDataList[m_ArchiveDataList.Count - 1];
                            //标记当前存档
                            MarkCurrentLoaded(m_CurrentArchiveData.TimeStamp);
                        }
                        else
                        {
                            foreach (var data in m_ArchiveTableList)
                            {
                                if (data.IsLoaded)
                                {
                                    m_CurrentArchiveData = m_ArchiveDataList.Find(x => x.TimeStamp == data.TimeStamp);
                                    break;
                                }
                            }
                        }
                        foreach (var data in m_ArchiveList)
                        {
                            data.SetData(m_CurrentArchiveData);
                        }
                    }
                    else
                    {
                        Debug.LogError("存档数据列表为空");
                    }
                }
                else
                {
                    SaveMultiple();
                    Debug.LogError($"存档表文件不存在: {archiveTablePath}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"读取存档失败: {ex.Message}");
            }
        }
        private UniTask LoadMultipleAsync()
        {
            return UniTask.RunOnThreadPool(() =>
            {
                LoadMultiple();
            });
        }
        /// <summary>
        /// 删除当前存档，适用于单存档模式
        /// </summary>
        public void Delete()
        {
            try
            {
                string childFolder = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData.GetType());
                if (string.IsNullOrEmpty(childFolder))
                {
                    Debug.LogError("获取子文件夹失败，存档删除失败");
                    return;
                }
                string filePath = Path.Combine(childFolder, m_ArchiveSetting.FileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log("存档删除成功");
                }
                else
                {
                    Debug.LogError($"存档文件不存在: {filePath}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"删除存档失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 删除指定时间戳的存档，适用于多存档模式
        /// </summary>
        /// <param name="timestamp"></param>
        public void Delete(long timestamp)
        {
            try
            {
                string childFolder = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData.GetType());
                if (string.IsNullOrEmpty(childFolder))
                {
                    Debug.LogError("获取子文件夹失败，存档删除失败");
                    return;
                }
                string filePath = Path.Combine(childFolder, $"{timestamp}.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    m_ArchiveTableList.RemoveAll(x => x.TimeStamp == timestamp);
                    m_ArchiveDataList.RemoveAll(x => x.TimeStamp == timestamp);
                    // 更新存档表文件
                    string json = JsonConvert.SerializeObject(m_ArchiveTableList);
                    using (StreamWriter sw = new StreamWriter(Path.Combine(m_MainFolderPath, ArchiveSetting.ARCHIVETABLE), false))
                    {
                        sw.Write(json);
                    }
                    Debug.Log("存档删除成功");
                }
                else
                {
                    Debug.LogError($"存档文件不存在: {filePath}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"删除存档失败: {ex.Message}");
            }
        }

        private void MarkCurrentLoaded(long timestamp)
        {
            foreach (var data in m_ArchiveTableList)
            {
                if (data.TimeStamp == timestamp)
                {
                    data.IsLoaded = true;
                }
                else
                {
                    data.IsLoaded = false;
                }
            }
        }
        private bool HasMarkedLoaded()
        {
            foreach (var data in m_ArchiveTableList)
            {
                if (data.IsLoaded)
                {
                    return true;
                }
            }
            return false;
        }

        public interface IArchive
        {
            void Register(IArchive data) => Instance.Register(data);
            void Unregister(IArchive data) => Instance.Unregister(data);
            void GetData(T1 data);
            void SetData(T1 data);
        }
    }
    [DefaultExecutionOrder(-800)]
    public class ArchiveSystem<T1, T2, U> : MonoBehaviour where T1 : ArchiveDataBase, new() where T2 : ArchiveDataBase, new() where U : ArchiveTableBase, new()
    {
        private static ArchiveSystem<T1, T2, U> _instance;
        public static ArchiveSystem<T1, T2, U> Instance => _instance;
        private ArchiveSetting m_ArchiveSetting => ArchiveSetting.Instance;
        private readonly List<IArchive> m_ArchiveList = new List<IArchive>();
        [SerializeField]
        private List<U> m_ArchiveTableList = new List<U>();
        [SerializeField]
        private List<T1> m_ArchiveDataList1 = new List<T1>();
        [SerializeField]
        private List<T2> m_ArchiveDataList2 = new List<T2>();
        [SerializeField]
        private T1 m_CurrentArchiveData1 = new T1();
        public T1 CurrentArchiveData1 => m_CurrentArchiveData1;
        [SerializeField]
        private T2 m_CurrentArchiveData2 = new T2();
        public T2 CurrentArchiveData2 => m_CurrentArchiveData2;
        private string m_MainFolderPath;

        /// <summary>
        /// 错误事件
        /// </summary>
        public event EventHandler<ArchiveErrorEventArgs> OnError;

        /// <summary>
        /// 触发错误事件
        /// </summary>
        /// <param name="errorType">错误类型</param>
        /// <param name="errorMessage">错误消息</param>
        /// <param name="exception">异常对象</param>
        protected void RaiseError(ArchiveErrorType errorType, string errorMessage, Exception exception = null)
        {
            OnError?.Invoke(this, new ArchiveErrorEventArgs
            {
                ErrorType = errorType,
                ErrorMessage = errorMessage,
                Exception = exception
            });
            Debug.LogError($"存档系统错误 ({errorType}): {errorMessage}");
            if (exception != null)
            {
                Debug.LogError(exception);
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
            if (ArchiveSystemHelper.TryCreateMainFolderFolder(m_ArchiveSetting, out m_MainFolderPath))
            {
                Debug.Log($"存档主文件夹路径: {m_MainFolderPath}");
            }
            else
            {
                Debug.LogError("创建存档主文件夹失败");
            }
        }
        public void Register(IArchive data)
        {
            if (!m_ArchiveList.Contains(data))
            {
                m_ArchiveList.Add(data);
                data.GetData(m_CurrentArchiveData1, m_CurrentArchiveData2);
            }
        }
        public void Unregister(IArchive data)
        {
            if (m_ArchiveList.Contains(data))
            {
                m_ArchiveList.Remove(data);
                data.SetData(m_CurrentArchiveData1, m_CurrentArchiveData2);
            }
        }
        /// <summary>
        /// 保存存档
        /// </summary>
        public static void Save()
        {
            switch (Instance.m_ArchiveSetting.ArchiveType)
            {
                case ArchiveType.Single:
                    Instance.SaveSingle();
                    break;
                case ArchiveType.Multiple:
                    Instance.SaveMultiple();
                    break;
            }
        }
        /// <summary>
        /// 异步保存存档
        /// </summary>
        /// <returns></returns>
        public static UniTask SaveAsync()
        {
            switch (Instance.m_ArchiveSetting.ArchiveType)
            {
                case ArchiveType.Single:
                    return Instance.SaveSingleAsync();
                case ArchiveType.Multiple:
                    return Instance.SaveMultipleAsync();
                default:
                    return UniTask.CompletedTask;
            }
        }
        /// <summary>
        /// 加载存档
        /// </summary>
        public static void Load()
        {
            switch (Instance.m_ArchiveSetting.ArchiveType)
            {
                case ArchiveType.Single:
                    Instance.LoadSingle();
                    break;
                case ArchiveType.Multiple:
                    Instance.LoadMultiple();
                    break;
            }
        }
        /// <summary>
        /// 异步加载存档
        /// </summary>
        /// <returns></returns>
        public static UniTask LoadAsync()
        {
            switch (Instance.m_ArchiveSetting.ArchiveType)
            {
                case ArchiveType.Single:
                    return Instance.LoadSingleAsync();
                case ArchiveType.Multiple:
                    return Instance.LoadMultipleAsync();
                default:
                    return UniTask.CompletedTask;
            }
        }
        public static void Load(long timestamp)
        {
            try
            {
                string childFolder1 = ArchiveSystemHelper.GetChildFolder(Instance.m_MainFolderPath, Instance.m_CurrentArchiveData1.GetType());
                string childFolder2 = ArchiveSystemHelper.GetChildFolder(Instance.m_MainFolderPath, Instance.m_CurrentArchiveData2.GetType());
                if (string.IsNullOrEmpty(childFolder1) || string.IsNullOrEmpty(childFolder2))
                {
                    Debug.LogError("获取子文件夹失败，存档加载失败");
                    return;
                }
                string filePath1 = Path.Combine(childFolder1, $"{timestamp}.json");
                string filePath2 = Path.Combine(childFolder2, $"{timestamp}.json");
                bool file1Exists = File.Exists(filePath1);
                bool file2Exists = File.Exists(filePath2);
                if (file1Exists || file2Exists)
                {
                    if (file1Exists)
                    {
                        using (StreamReader sr = new StreamReader(filePath1))
                        {
                            string json = sr.ReadToEnd();
                            if (Instance.m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                            {
                                json = Rijindael.Decrypt(json, Instance.m_ArchiveSetting.KEY, Instance.m_ArchiveSetting.IV);
                            }
                            Instance.m_CurrentArchiveData1 = JsonConvert.DeserializeObject<T1>(json);
                        }
                    }
                    if (file2Exists)
                    {
                        using (StreamReader sr = new StreamReader(filePath2))
                        {
                            string json = sr.ReadToEnd();
                            if (Instance.m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                            {
                                json = Rijindael.Decrypt(json, Instance.m_ArchiveSetting.KEY, Instance.m_ArchiveSetting.IV);
                            }
                            Instance.m_CurrentArchiveData2 = JsonConvert.DeserializeObject<T2>(json);
                        }
                    }
                    foreach (var data in Instance.m_ArchiveList)
                    {
                        data.SetData(Instance.m_CurrentArchiveData1, Instance.m_CurrentArchiveData2);
                    }
                }
                else
                {
                    Debug.LogError($"存档文件不存在: {filePath1} 和 {filePath2}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"读取存档失败: {ex.Message}");
            }
        }
        public static UniTask LoadAsync(long timestamp)
        {
            return UniTask.RunOnThreadPool(() =>
            {
                Load(timestamp);
            });
        }
        private void SaveSingle()
        {
            try
            {
                foreach (var data in m_ArchiveList)
                {
                    data.GetData(m_CurrentArchiveData1, m_CurrentArchiveData2);
                }
                string json1 = JsonConvert.SerializeObject(m_CurrentArchiveData1);
                string json2 = JsonConvert.SerializeObject(m_CurrentArchiveData2);
                if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                {
                    json1 = Rijindael.Encrypt(json1, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                    json2 = Rijindael.Encrypt(json2, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                }
                string childFolder1 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData1.GetType());
                string childFolder2 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData2.GetType());
                if (string.IsNullOrEmpty(childFolder1) || string.IsNullOrEmpty(childFolder2))
                {
                    Debug.LogError("获取子文件夹失败，存档保存失败");
                    return;
                }
                string filePath1 = Path.Combine(childFolder1, $"{m_ArchiveSetting.FileName}");
                string filePath2 = Path.Combine(childFolder2, $"{m_ArchiveSetting.FileName}");
                if (File.Exists(filePath1))
                {
                    File.Delete(filePath1);
                }
                if (File.Exists(filePath2))
                {
                    File.Delete(filePath2);
                }
                using (StreamWriter sw = new StreamWriter(filePath1, false))
                {
                    sw.Write(json1);
                }
                using (StreamWriter sw = new StreamWriter(filePath2, false))
                {
                    sw.Write(json2);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"存档失败: {ex.Message}");
            }
        }
        private async UniTask SaveSingleAsync()
        {
            await UniTask.RunOnThreadPool(async () =>
            {
                try
                {
                    foreach (var data in m_ArchiveList)
                    {
                        data.GetData(m_CurrentArchiveData1, m_CurrentArchiveData2);
                    }
                    string json1 = JsonConvert.SerializeObject(m_CurrentArchiveData1);
                    string json2 = JsonConvert.SerializeObject(m_CurrentArchiveData2);
                    if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                    {
                        json1 = Rijindael.Encrypt(json1, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                        json2 = Rijindael.Encrypt(json2, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                    }
                    string childFolder1 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData1.GetType());
                    string childFolder2 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData2.GetType());
                    if (string.IsNullOrEmpty(childFolder1) || string.IsNullOrEmpty(childFolder2))
                    {
                        Debug.LogError("获取子文件夹失败，存档保存失败");
                        return;
                    }
                    string filePath1 = Path.Combine(childFolder1, $"{m_ArchiveSetting.FileName}");
                    string filePath2 = Path.Combine(childFolder2, $"{m_ArchiveSetting.FileName}");
                    if (File.Exists(filePath1))
                    {
                        File.Delete(filePath1);
                    }
                    if (File.Exists(filePath2))
                    {
                        File.Delete(filePath2);
                    }
                    using (StreamWriter sw = new StreamWriter(filePath1, false))
                    {
                        await sw.WriteAsync(json1);
                    }
                    using (StreamWriter sw = new StreamWriter(filePath2, false))
                    {
                        await sw.WriteAsync(json2);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"存档失败: {ex.Message}");
                }
            });
        }
        private void LoadSingle()
        {
            try
            {
                string childFolder1 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData1.GetType());
                string childFolder2 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData2.GetType());
                if (string.IsNullOrEmpty(childFolder1) || string.IsNullOrEmpty(childFolder2))
                {
                    Debug.LogError("获取子文件夹失败，存档加载失败");
                    return;
                }
                string filePath1 = Path.Combine(childFolder1, $"{m_ArchiveSetting.FileName}");
                string filePath2 = Path.Combine(childFolder2, $"{m_ArchiveSetting.FileName}");
                if (File.Exists(filePath1))
                {
                    using (StreamReader sr = new StreamReader(filePath1))
                    {
                        string json = sr.ReadToEnd();
                        if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                        {
                            json = Rijindael.Decrypt(json, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                        }
                        m_CurrentArchiveData1 = JsonConvert.DeserializeObject<T1>(json);
                    }
                }
                if (File.Exists(filePath2))
                {
                    using (StreamReader sr = new StreamReader(filePath2))
                    {
                        string json = sr.ReadToEnd();
                        if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                        {
                            json = Rijindael.Decrypt(json, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                        }
                        m_CurrentArchiveData2 = JsonConvert.DeserializeObject<T2>(json);
                    }
                }
                foreach (var data in m_ArchiveList)
                {
                    data.SetData(m_CurrentArchiveData1, m_CurrentArchiveData2);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"读取存档失败: {ex.Message}");
            }
        }
        private UniTask LoadSingleAsync()
        {
            return UniTask.RunOnThreadPool(() =>
            {
                LoadSingle();
            });
        }
        private void SaveMultiple()
        {
            try
            {
                U archiveTable = new U();
                m_CurrentArchiveData1 = new T1();
                m_CurrentArchiveData2 = new T2();
                foreach (var data in m_ArchiveList)
                {
                    data.GetData(m_CurrentArchiveData1, m_CurrentArchiveData2);
                }
                m_CurrentArchiveData1.TimeStamp = archiveTable.TimeStamp;
                m_CurrentArchiveData2.TimeStamp = archiveTable.TimeStamp;
                int existingIndex = m_ArchiveTableList.FindIndex(t => t.TimeStamp == archiveTable.TimeStamp);
                if (existingIndex > 0)
                {
                    m_ArchiveTableList[existingIndex] = archiveTable;
                    m_ArchiveDataList1[existingIndex] = m_CurrentArchiveData1;
                    m_ArchiveDataList2[existingIndex] = m_CurrentArchiveData2;
                }
                else
                {
                    m_ArchiveTableList.Add(archiveTable);
                    m_ArchiveDataList1.Add(m_CurrentArchiveData1);
                    m_ArchiveDataList2.Add(m_CurrentArchiveData2);
                }
                MarkCurrentLoaded(m_CurrentArchiveData1.TimeStamp);
                string json1 = JsonConvert.SerializeObject(m_ArchiveTableList);
                using (StreamWriter sw = new StreamWriter(Path.Combine(m_MainFolderPath, ArchiveSetting.ARCHIVETABLE), false))
                {
                    sw.Write(json1);
                }
                string json2 = JsonConvert.SerializeObject(m_CurrentArchiveData1);
                string json3 = JsonConvert.SerializeObject(m_CurrentArchiveData2);
                if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                {
                    json2 = Rijindael.Encrypt(json2, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                    json3 = Rijindael.Encrypt(json3, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                }
                string childFolder1 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData1.GetType());
                string childFolder2 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData2.GetType());
                if (string.IsNullOrEmpty(childFolder1) || string.IsNullOrEmpty(childFolder2))
                {
                    Debug.LogError("获取子文件夹失败，存档保存失败");
                    return;
                }
                string filePath1 = Path.Combine(childFolder1, $"{archiveTable.TimeStamp}.json");
                string filePath2 = Path.Combine(childFolder2, $"{archiveTable.TimeStamp}.json");
                if (File.Exists(filePath1))
                {
                    File.Delete(filePath1);
                }
                if (File.Exists(filePath2))
                {
                    File.Delete(filePath2);
                }
                using (StreamWriter sw = new StreamWriter(filePath1, false))
                {
                    sw.Write(json2);
                }
                using (StreamWriter sw = new StreamWriter(filePath2, false))
                {
                    sw.Write(json3);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"存档失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 覆写存档，传入时间戳参数以指定存档文件名，适用于多存档模式
        /// </summary>
        /// <param name="timeStamp"></param>
        public void Save(long timeStamp)
        {
            try
            {
                U archiveTable = new U()
                {
                    TimeStamp = timeStamp,
                };
                m_CurrentArchiveData1 = new T1();
                m_CurrentArchiveData2 = new T2();
                foreach (var data in m_ArchiveList)
                {
                    data.GetData(m_CurrentArchiveData1, m_CurrentArchiveData2);
                }
                m_CurrentArchiveData1.TimeStamp = archiveTable.TimeStamp;
                m_CurrentArchiveData2.TimeStamp = archiveTable.TimeStamp;
                int existingIndex = m_ArchiveTableList.FindIndex(t => t.TimeStamp == archiveTable.TimeStamp);
                if (existingIndex > 0)
                {
                    m_ArchiveTableList[existingIndex] = archiveTable;
                    m_ArchiveDataList1[existingIndex] = m_CurrentArchiveData1;
                    m_ArchiveDataList2[existingIndex] = m_CurrentArchiveData2;
                }
                else
                {
                    m_ArchiveTableList.Add(archiveTable);
                    m_ArchiveDataList1.Add(m_CurrentArchiveData1);
                    m_ArchiveDataList2.Add(m_CurrentArchiveData2);
                }
                MarkCurrentLoaded(m_CurrentArchiveData1.TimeStamp);
                string json1 = JsonConvert.SerializeObject(m_ArchiveTableList);
                using (StreamWriter sw = new StreamWriter(Path.Combine(m_MainFolderPath, ArchiveSetting.ARCHIVETABLE), false))
                {
                    sw.Write(json1);
                }
                string json2 = JsonConvert.SerializeObject(m_CurrentArchiveData1);
                string json3 = JsonConvert.SerializeObject(m_CurrentArchiveData2);
                if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                {
                    json2 = Rijindael.Encrypt(json2, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                    json3 = Rijindael.Encrypt(json3, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                }
                string childFolder1 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData1.GetType());
                string childFolder2 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData2.GetType());
                if (string.IsNullOrEmpty(childFolder1) || string.IsNullOrEmpty(childFolder2))
                {
                    Debug.LogError("获取子文件夹失败，存档保存失败");
                    return;
                }
                string filePath1 = Path.Combine(childFolder1, $"{archiveTable.TimeStamp}.json");
                string filePath2 = Path.Combine(childFolder2, $"{archiveTable.TimeStamp}.json");
                if (File.Exists(filePath1))
                {
                    File.Delete(filePath1);
                }
                if (File.Exists(filePath2))
                {
                    File.Delete(filePath2);
                }
                using (StreamWriter sw = new StreamWriter(filePath1, false))
                {
                    sw.Write(json2);
                }
                using (StreamWriter sw = new StreamWriter(filePath2, false))
                {
                    sw.Write(json3);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"存档失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 异步保存存档，传入时间戳参数以指定存档文件名，适用于多存档模式
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <returns></returns>
        public async UniTask SaveAsync(long timeStamp)
        {
            await UniTask.RunOnThreadPool(async () =>
            {
                try
                {
                    U archiveTable = new U()
                    {
                        TimeStamp = timeStamp,
                    };
                    m_CurrentArchiveData1 = new T1();
                    m_CurrentArchiveData2 = new T2();
                    foreach (var data in m_ArchiveList)
                    {
                        data.GetData(m_CurrentArchiveData1, m_CurrentArchiveData2);
                    }
                    m_CurrentArchiveData1.TimeStamp = archiveTable.TimeStamp;
                    m_CurrentArchiveData2.TimeStamp = archiveTable.TimeStamp;
                    int existingIndex = m_ArchiveTableList.FindIndex(t => t.TimeStamp == archiveTable.TimeStamp);
                    if (existingIndex >= 0)
                    {
                        m_ArchiveTableList[existingIndex] = archiveTable;
                        m_ArchiveDataList1[existingIndex] = m_CurrentArchiveData1;
                        m_ArchiveDataList2[existingIndex] = m_CurrentArchiveData2;
                    }
                    else
                    {
                        m_ArchiveTableList.Add(archiveTable);
                        m_ArchiveDataList1.Add(m_CurrentArchiveData1);
                        m_ArchiveDataList2.Add(m_CurrentArchiveData2);
                    }
                    MarkCurrentLoaded(m_CurrentArchiveData1.TimeStamp);
                    string json1 = JsonConvert.SerializeObject(m_ArchiveTableList);
                    using (StreamWriter sw = new StreamWriter(Path.Combine(m_MainFolderPath, ArchiveSetting.ARCHIVETABLE), false))
                    {
                        await sw.WriteAsync(json1);
                    }
                    string json2 = JsonConvert.SerializeObject(m_CurrentArchiveData1);
                    string json3 = JsonConvert.SerializeObject(m_CurrentArchiveData2);
                    if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                    {
                        json2 = Rijindael.Encrypt(json2, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                        json3 = Rijindael.Encrypt(json3, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                    }
                    string filePath1 = Path.Combine(ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData1.GetType()), $"{archiveTable.TimeStamp}.json");
                    string filePath2 = Path.Combine(ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData2.GetType()), $"{archiveTable.TimeStamp}.json");
                    if (File.Exists(filePath1))
                    {
                        File.Delete(filePath1);
                    }
                    if (File.Exists(filePath2))
                    {
                        File.Delete(filePath2);
                    }
                    using (StreamWriter sw = new StreamWriter(filePath1, false))
                    {
                        await sw.WriteAsync(json2);
                    }
                    using (StreamWriter sw = new StreamWriter(filePath2, false))
                    {
                        await sw.WriteAsync(json3);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"存档失败: {ex.Message}");
                }
            });
        }
        private async UniTask SaveMultipleAsync()
        {
            await UniTask.RunOnThreadPool(async () =>
            {
                try
                {
                    U archiveTable = new U();
                    m_CurrentArchiveData1 = new T1();
                    m_CurrentArchiveData2 = new T2();
                    foreach (var data in m_ArchiveList)
                    {
                        data.GetData(m_CurrentArchiveData1, m_CurrentArchiveData2);
                    }
                    m_CurrentArchiveData1.TimeStamp = archiveTable.TimeStamp;
                    m_CurrentArchiveData2.TimeStamp = archiveTable.TimeStamp;
                    int existingIndex = m_ArchiveTableList.FindIndex(t => t.TimeStamp == archiveTable.TimeStamp);
                    if (existingIndex >= 0)
                    {
                        m_ArchiveTableList[existingIndex] = archiveTable;
                        m_ArchiveDataList1[existingIndex] = m_CurrentArchiveData1;
                        m_ArchiveDataList2[existingIndex] = m_CurrentArchiveData2;
                    }
                    else
                    {
                        m_ArchiveTableList.Add(archiveTable);
                        m_ArchiveDataList1.Add(m_CurrentArchiveData1);
                        m_ArchiveDataList2.Add(m_CurrentArchiveData2);
                    }
                    MarkCurrentLoaded(m_CurrentArchiveData1.TimeStamp);
                    string json1 = JsonConvert.SerializeObject(m_ArchiveTableList);
                    using (StreamWriter sw = new StreamWriter(Path.Combine(m_MainFolderPath, ArchiveSetting.ARCHIVETABLE), false))
                    {
                        await sw.WriteAsync(json1);
                    }
                    string json2 = JsonConvert.SerializeObject(m_CurrentArchiveData1);
                    string json3 = JsonConvert.SerializeObject(m_CurrentArchiveData2);
                    if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                    {
                        json2 = Rijindael.Encrypt(json2, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                        json3 = Rijindael.Encrypt(json3, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                    }
                    string filePath1 = Path.Combine(ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData1.GetType()), $"{archiveTable.TimeStamp}.json");
                    string filePath2 = Path.Combine(ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData2.GetType()), $"{archiveTable.TimeStamp}.json");
                    if (File.Exists(filePath1))
                    {
                        File.Delete(filePath1);
                    }
                    if (File.Exists(filePath2))
                    {
                        File.Delete(filePath2);
                    }
                    using (StreamWriter sw = new StreamWriter(filePath1, false))
                    {
                        await sw.WriteAsync(json2);
                    }
                    using (StreamWriter sw = new StreamWriter(filePath2, false))
                    {
                        await sw.WriteAsync(json3);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"存档失败: {ex.Message}");
                }
            });
        }
        private void LoadMultiple()
        {
            try
            {
                string archiveTablePath = Path.Combine(m_MainFolderPath, ArchiveSetting.ARCHIVETABLE);
                if (File.Exists(archiveTablePath))
                {
                    using (StreamReader sr = new StreamReader(archiveTablePath))
                    {
                        string json = sr.ReadToEnd();
                        m_ArchiveTableList = JsonConvert.DeserializeObject<List<U>>(json);
                    }
                    m_ArchiveDataList1.Clear();
                    m_ArchiveDataList2.Clear();
                    string childFolder1 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData1.GetType());
                    string childFolder2 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData2.GetType());
                    if (string.IsNullOrEmpty(childFolder1) || string.IsNullOrEmpty(childFolder2))
                    {
                        Debug.LogError("获取子文件夹失败，存档加载失败");
                        return;
                    }
                    foreach (var file in m_ArchiveTableList)
                    {
                        string archiveDataPath1 = Path.Combine(childFolder1, $"{file.TimeStamp}.json");
                        string archiveDataPath2 = Path.Combine(childFolder2, $"{file.TimeStamp}.json");
                        if (File.Exists(archiveDataPath1))
                        {
                            using (StreamReader sr2 = new StreamReader(archiveDataPath1))
                            {
                                string json2 = sr2.ReadToEnd();
                                if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                                {
                                    json2 = Rijindael.Decrypt(json2, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                                }
                                m_ArchiveDataList1.Add(JsonConvert.DeserializeObject<T1>(json2));
                            }
                        }
                        if (File.Exists(archiveDataPath2))
                        {
                            using (StreamReader sr2 = new StreamReader(archiveDataPath2))
                            {
                                string json2 = sr2.ReadToEnd();
                                if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                                {
                                    json2 = Rijindael.Decrypt(json2, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                                }
                                m_ArchiveDataList2.Add(JsonConvert.DeserializeObject<T2>(json2));
                            }
                        }
                    }
                    if (m_ArchiveDataList1.Count > 0 && m_ArchiveDataList2.Count > 0)
                    {
                        if (!HasMarkedLoaded())
                        {
                            m_CurrentArchiveData1 = m_ArchiveDataList1[m_ArchiveDataList1.Count - 1];
                            m_CurrentArchiveData2 = m_ArchiveDataList2[m_ArchiveDataList2.Count - 1];
                            MarkCurrentLoaded(m_CurrentArchiveData1.TimeStamp);
                        }
                        else
                        {
                            foreach (var data in m_ArchiveTableList)
                            {
                                if (data.IsLoaded)
                                {
                                    m_CurrentArchiveData1 = m_ArchiveDataList1.Find(x => x.TimeStamp == data.TimeStamp);
                                    m_CurrentArchiveData2 = m_ArchiveDataList2.Find(x => x.TimeStamp == data.TimeStamp);
                                    break;
                                }
                            }
                        }
                        foreach (var data in m_ArchiveList)
                        {
                            data.SetData(m_CurrentArchiveData1, m_CurrentArchiveData2);
                        }
                    }
                    else
                    {
                        Debug.LogError("存档数据列表为空");
                    }
                }
                else
                {
                    Debug.LogError($"存档表文件不存在: {archiveTablePath}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"读取存档失败: {ex.Message}");
            }
        }
        private UniTask LoadMultipleAsync()
        {
            return UniTask.RunOnThreadPool(() =>
            {
                LoadMultiple();
            });
        }
        /// <summary>
        /// 删除当前存档，适用于单存档模式
        /// </summary>
        public void Delete()
        {
            try
            {
                string childFolder1 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData1.GetType());
                string childFolder2 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData2.GetType());
                if (string.IsNullOrEmpty(childFolder1) || string.IsNullOrEmpty(childFolder2))
                {
                    Debug.LogError("获取子文件夹失败，存档删除失败");
                    return;
                }
                string filePath1 = Path.Combine(childFolder1, $"{m_ArchiveSetting.FileName}");
                string filePath2 = Path.Combine(childFolder2, $"{m_ArchiveSetting.FileName}");
                if (File.Exists(filePath1))
                {
                    File.Delete(filePath1);
                }
                if (File.Exists(filePath2))
                {
                    File.Delete(filePath2);
                }
                Debug.Log("存档删除成功");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"删除存档失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 删除指定时间戳的存档，适用于多存档模式
        /// </summary>
        /// <param name="timestamp"></param>
        public void Delete(long timestamp)
        {
            try
            {
                string childFolder1 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData1.GetType());
                string childFolder2 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData2.GetType());
                if (string.IsNullOrEmpty(childFolder1) || string.IsNullOrEmpty(childFolder2))
                {
                    Debug.LogError("获取子文件夹失败，存档删除失败");
                    return;
                }
                string filePath1 = Path.Combine(childFolder1, $"{timestamp}.json");
                string filePath2 = Path.Combine(childFolder2, $"{timestamp}.json");
                if (File.Exists(filePath1))
                {
                    File.Delete(filePath1);
                }
                if (File.Exists(filePath2))
                {
                    File.Delete(filePath2);
                }
                m_ArchiveTableList.RemoveAll(x => x.TimeStamp == timestamp);
                m_ArchiveDataList1.RemoveAll(x => x.TimeStamp == timestamp);
                m_ArchiveDataList2.RemoveAll(x => x.TimeStamp == timestamp);
                // 更新存档表文件
                string json = JsonConvert.SerializeObject(m_ArchiveTableList);
                using (StreamWriter sw = new StreamWriter(Path.Combine(m_MainFolderPath, ArchiveSetting.ARCHIVETABLE), false))
                {
                    sw.Write(json);
                }
                Debug.Log("存档删除成功");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"删除存档失败: {ex.Message}");
            }
        }
        private void MarkCurrentLoaded(long timestamp)
        {
            foreach (var data in m_ArchiveTableList)
            {
                if (data.TimeStamp == timestamp)
                {
                    data.IsLoaded = true;
                }
                else
                {
                    data.IsLoaded = false;
                }
            }
        }
        private bool HasMarkedLoaded()
        {
            foreach (var data in m_ArchiveTableList)
            {
                if (data.IsLoaded)
                {
                    return true;
                }
            }
            return false;
        }
        public interface IArchive
        {
            /// <summary>
            /// 注册存档数据
            /// </summary>
            /// <param name="data"></param>
            /// <returns></returns>
            void Register(IArchive data) => Instance.Register(data);
            /// <summary>
            /// 取消注册存档数据
            /// </summary>
            /// <param name="data"></param>
            /// <returns></returns>
            void Unregister(IArchive data) => Instance.Unregister(data);
            /// <summary>
            /// 如果一个物体常驻场景则此方法将数据传到存档系统
            /// </summary>
            /// <remarks>若物体为数据生成，则此方法为从存档获得数据</remarks>
            /// <param name="data1"></param>
            /// <param name="data2"></param>
            void GetData(T1 data1, T2 data2);
            /// <summary>
            /// 如果一个物体常驻场景则此方法从存档系统获取数据 
            /// </summary>
            /// <remarks>若物体为数据生成，则此方法将数据传到存档系统</remarks>
            /// <param name="data1"></param>
            /// <param name="data2"></param>
            void SetData(T1 data1, T2 data2);
        }
    }

    [DefaultExecutionOrder(-800)]
    public class ArchiveSystem<T1, T2, T3, U> : MonoBehaviour where T1 : ArchiveDataBase, new() where T2 : ArchiveDataBase, new() where T3 : ArchiveDataBase, new() where U : ArchiveTableBase, new()
    {
        private static ArchiveSystem<T1, T2, T3, U> _instance;
        public static ArchiveSystem<T1, T2, T3, U> Instance => _instance;
        private ArchiveSetting m_ArchiveSetting => ArchiveSetting.Instance;
        private readonly List<IArchive> m_ArchiveList = new List<IArchive>();
        [SerializeField]
        private List<U> m_ArchiveTableList = new List<U>();
        [SerializeField]
        private List<T1> m_ArchiveDataList1 = new List<T1>();
        [SerializeField]
        private List<T2> m_ArchiveDataList2 = new List<T2>();
        [SerializeField]
        private List<T3> m_ArchiveDataList3 = new List<T3>();
        [SerializeField]
        private T1 m_CurrentArchiveData1 = new T1();
        public T1 CurrentArchiveData1 => m_CurrentArchiveData1;
        [SerializeField]
        private T2 m_CurrentArchiveData2 = new T2();
        public T2 CurrentArchiveData2 => m_CurrentArchiveData2;
        [SerializeField]
        private T3 m_CurrentArchiveData3 = new T3();
        public T3 CurrentArchiveData3 => m_CurrentArchiveData3;
        private string m_MainFolderPath;

        /// <summary>
        /// 错误事件
        /// </summary>
        public event EventHandler<ArchiveErrorEventArgs> OnError;

        /// <summary>
        /// 触发错误事件
        /// </summary>
        /// <param name="errorType">错误类型</param>
        /// <param name="errorMessage">错误消息</param>
        /// <param name="exception">异常对象</param>
        protected void RaiseError(ArchiveErrorType errorType, string errorMessage, Exception exception = null)
        {
            OnError?.Invoke(this, new ArchiveErrorEventArgs
            {
                ErrorType = errorType,
                ErrorMessage = errorMessage,
                Exception = exception
            });
            Debug.LogError($"存档系统错误 ({errorType}): {errorMessage}");
            if (exception != null)
            {
                Debug.LogError(exception);
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
            if (ArchiveSystemHelper.TryCreateMainFolderFolder(m_ArchiveSetting, out m_MainFolderPath))
            {
                Debug.Log($"存档主文件夹路径: {m_MainFolderPath}");
            }
            else
            {
                Debug.LogError("创建存档主文件夹失败");
            }
        }
        public void Register(IArchive data)
        {
            if (!m_ArchiveList.Contains(data))
            {
                m_ArchiveList.Add(data);
                data.GetData(m_CurrentArchiveData1, m_CurrentArchiveData2, m_CurrentArchiveData3);
            }
        }
        public void Unregister(IArchive data)
        {
            if (m_ArchiveList.Contains(data))
            {
                m_ArchiveList.Remove(data);
                data.SetData(m_CurrentArchiveData1, m_CurrentArchiveData2, m_CurrentArchiveData3);
            }
        }
        /// <summary>
        /// 保存存档
        /// </summary>
        public static void Save()
        {
            switch (Instance.m_ArchiveSetting.ArchiveType)
            {
                case ArchiveType.Single:
                    Instance.SaveSingle();
                    break;
                case ArchiveType.Multiple:
                    Instance.SaveMultiple();
                    break;
            }
        }
        /// <summary>
        /// 异步保存存档
        /// </summary>
        /// <returns></returns>
        public static UniTask SaveAsync()
        {
            switch (Instance.m_ArchiveSetting.ArchiveType)
            {
                case ArchiveType.Single:
                    return Instance.SaveSingleAsync();
                case ArchiveType.Multiple:
                    return Instance.SaveMultipleAsync();
                default:
                    return UniTask.CompletedTask;
            }
        }
        /// <summary>
        /// 加载存档
        /// </summary>
        public static void Load()
        {
            switch (Instance.m_ArchiveSetting.ArchiveType)
            {
                case ArchiveType.Single:
                    Instance.LoadSingle();
                    break;
                case ArchiveType.Multiple:
                    Instance.LoadMultiple();
                    break;
            }
        }
        /// <summary>
        /// 异步加载存档
        /// </summary>
        /// <returns></returns>
        public static UniTask LoadAsync()
        {
            switch (Instance.m_ArchiveSetting.ArchiveType)
            {
                case ArchiveType.Single:
                    return Instance.LoadSingleAsync();
                case ArchiveType.Multiple:
                    return Instance.LoadMultipleAsync();
                default:
                    return UniTask.CompletedTask;
            }
        }
        public static void Load(long timestamp)
        {
            try
            {
                string childFolder1 = ArchiveSystemHelper.GetChildFolder(Instance.m_MainFolderPath, Instance.m_CurrentArchiveData1.GetType());
                string childFolder2 = ArchiveSystemHelper.GetChildFolder(Instance.m_MainFolderPath, Instance.m_CurrentArchiveData2.GetType());
                string childFolder3 = ArchiveSystemHelper.GetChildFolder(Instance.m_MainFolderPath, Instance.m_CurrentArchiveData3.GetType());
                if (string.IsNullOrEmpty(childFolder1) || string.IsNullOrEmpty(childFolder2) || string.IsNullOrEmpty(childFolder3))
                {
                    Debug.LogError("获取子文件夹失败，存档加载失败");
                    return;
                }
                string filePath1 = Path.Combine(childFolder1, $"{timestamp}.json");
                string filePath2 = Path.Combine(childFolder2, $"{timestamp}.json");
                string filePath3 = Path.Combine(childFolder3, $"{timestamp}.json");
                bool file1Exists = File.Exists(filePath1);
                bool file2Exists = File.Exists(filePath2);
                bool file3Exists = File.Exists(filePath3);
                if (file1Exists || file2Exists || file3Exists)
                {
                    if (file1Exists)
                    {
                        using (StreamReader sr = new StreamReader(filePath1))
                        {
                            string json = sr.ReadToEnd();
                            if (Instance.m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                            {
                                json = Rijindael.Decrypt(json, Instance.m_ArchiveSetting.KEY, Instance.m_ArchiveSetting.IV);
                            }
                            Instance.m_CurrentArchiveData1 = JsonConvert.DeserializeObject<T1>(json);
                        }
                    }
                    if (file2Exists)
                    {
                        using (StreamReader sr = new StreamReader(filePath2))
                        {
                            string json = sr.ReadToEnd();
                            if (Instance.m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                            {
                                json = Rijindael.Decrypt(json, Instance.m_ArchiveSetting.KEY, Instance.m_ArchiveSetting.IV);
                            }
                            Instance.m_CurrentArchiveData2 = JsonConvert.DeserializeObject<T2>(json);
                        }
                    }
                    if (file3Exists)
                    {
                        using (StreamReader sr = new StreamReader(filePath3))
                        {
                            string json = sr.ReadToEnd();
                            if (Instance.m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                            {
                                json = Rijindael.Decrypt(json, Instance.m_ArchiveSetting.KEY, Instance.m_ArchiveSetting.IV);
                            }
                            Instance.m_CurrentArchiveData3 = JsonConvert.DeserializeObject<T3>(json);
                        }
                    }
                    foreach (var data in Instance.m_ArchiveList)
                    {
                        data.SetData(Instance.m_CurrentArchiveData1, Instance.m_CurrentArchiveData2, Instance.m_CurrentArchiveData3);
                    }
                }
                else
                {
                    Debug.LogError($"存档文件不存在: {filePath1}, {filePath2} 和 {filePath3}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"读取存档失败: {ex.Message}");
            }
        }
        public static UniTask LoadAsync(long timestamp)
        {
            return UniTask.RunOnThreadPool(() =>
            {
                Load(timestamp);
            });
        }
        private void SaveSingle()
        {
            try
            {
                foreach (var data in m_ArchiveList)
                {
                    data.GetData(m_CurrentArchiveData1, m_CurrentArchiveData2, m_CurrentArchiveData3);
                }
                string json1 = JsonConvert.SerializeObject(m_CurrentArchiveData1);
                string json2 = JsonConvert.SerializeObject(m_CurrentArchiveData2);
                string json3 = JsonConvert.SerializeObject(m_CurrentArchiveData3);
                if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                {
                    json1 = Rijindael.Encrypt(json1, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                    json2 = Rijindael.Encrypt(json2, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                    json3 = Rijindael.Encrypt(json3, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                }
                string childFolder1 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData1.GetType());
                string childFolder2 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData2.GetType());
                string childFolder3 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData3.GetType());
                if (string.IsNullOrEmpty(childFolder1) || string.IsNullOrEmpty(childFolder2) || string.IsNullOrEmpty(childFolder3))
                {
                    Debug.LogError("获取子文件夹失败，存档保存失败");
                    return;
                }
                string filePath1 = Path.Combine(childFolder1, $"{m_ArchiveSetting.FileName}");
                string filePath2 = Path.Combine(childFolder2, $"{m_ArchiveSetting.FileName}");
                string filePath3 = Path.Combine(childFolder3, $"{m_ArchiveSetting.FileName}");
                if (File.Exists(filePath1))
                {
                    File.Delete(filePath1);
                }
                if (File.Exists(filePath2))
                {
                    File.Delete(filePath2);
                }
                if (File.Exists(filePath3))
                {
                    File.Delete(filePath3);
                }
                using (StreamWriter sw = new StreamWriter(filePath1, false))
                {
                    sw.Write(json1);
                }
                using (StreamWriter sw = new StreamWriter(filePath2, false))
                {
                    sw.Write(json2);
                }
                using (StreamWriter sw = new StreamWriter(filePath3, false))
                {
                    sw.Write(json3);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"存档失败: {ex.Message}");
            }
        }
        private async UniTask SaveSingleAsync()
        {
            await UniTask.RunOnThreadPool(async () =>
            {
                try
                {
                    foreach (var data in m_ArchiveList)
                    {
                        data.GetData(m_CurrentArchiveData1, m_CurrentArchiveData2, m_CurrentArchiveData3);
                    }
                    string json1 = JsonConvert.SerializeObject(m_CurrentArchiveData1);
                    string json2 = JsonConvert.SerializeObject(m_CurrentArchiveData2);
                    string json3 = JsonConvert.SerializeObject(m_CurrentArchiveData3);
                    if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                    {
                        json1 = Rijindael.Encrypt(json1, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                        json2 = Rijindael.Encrypt(json2, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                        json3 = Rijindael.Encrypt(json3, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                    }
                    string childFolder1 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData1.GetType());
                    string childFolder2 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData2.GetType());
                    string childFolder3 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData3.GetType());
                    if (string.IsNullOrEmpty(childFolder1) || string.IsNullOrEmpty(childFolder2) || string.IsNullOrEmpty(childFolder3))
                    {
                        Debug.LogError("获取子文件夹失败，存档保存失败");
                        return;
                    }
                    string filePath1 = Path.Combine(childFolder1, $"{m_ArchiveSetting.FileName}");
                    string filePath2 = Path.Combine(childFolder2, $"{m_ArchiveSetting.FileName}");
                    string filePath3 = Path.Combine(childFolder3, $"{m_ArchiveSetting.FileName}");
                    if (File.Exists(filePath1))
                    {
                        File.Delete(filePath1);
                    }
                    if (File.Exists(filePath2))
                    {
                        File.Delete(filePath2);
                    }
                    if (File.Exists(filePath3))
                    {
                        File.Delete(filePath3);
                    }
                    using (StreamWriter sw = new StreamWriter(filePath1, false))
                    {
                        await sw.WriteAsync(json1);
                    }
                    using (StreamWriter sw = new StreamWriter(filePath2, false))
                    {
                        await sw.WriteAsync(json2);
                    }
                    using (StreamWriter sw = new StreamWriter(filePath3, false))
                    {
                        await sw.WriteAsync(json3);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"存档失败: {ex.Message}");
                }
            });
        }
        private void LoadSingle()
        {
            try
            {
                string childFolder1 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData1.GetType());
                string childFolder2 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData2.GetType());
                string childFolder3 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData3.GetType());
                if (string.IsNullOrEmpty(childFolder1) || string.IsNullOrEmpty(childFolder2) || string.IsNullOrEmpty(childFolder3))
                {
                    Debug.LogError("获取子文件夹失败，存档加载失败");
                    return;
                }
                string filePath1 = Path.Combine(childFolder1, $"{m_ArchiveSetting.FileName}");
                string filePath2 = Path.Combine(childFolder2, $"{m_ArchiveSetting.FileName}");
                string filePath3 = Path.Combine(childFolder3, $"{m_ArchiveSetting.FileName}");
                if (File.Exists(filePath1))
                {
                    using (StreamReader sr = new StreamReader(filePath1))
                    {
                        string json = sr.ReadToEnd();
                        if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                        {
                            json = Rijindael.Decrypt(json, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                        }
                        m_CurrentArchiveData1 = JsonConvert.DeserializeObject<T1>(json);
                    }
                }
                if (File.Exists(filePath2))
                {
                    using (StreamReader sr = new StreamReader(filePath2))
                    {
                        string json = sr.ReadToEnd();
                        if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                        {
                            json = Rijindael.Decrypt(json, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                        }
                        m_CurrentArchiveData2 = JsonConvert.DeserializeObject<T2>(json);
                    }
                }
                if (File.Exists(filePath3))
                {
                    using (StreamReader sr = new StreamReader(filePath3))
                    {
                        string json = sr.ReadToEnd();
                        if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                        {
                            json = Rijindael.Decrypt(json, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                        }
                        m_CurrentArchiveData3 = JsonConvert.DeserializeObject<T3>(json);
                    }
                }
                foreach (var data in m_ArchiveList)
                {
                    data.SetData(m_CurrentArchiveData1, m_CurrentArchiveData2, m_CurrentArchiveData3);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"读取存档失败: {ex.Message}");
            }
        }
        private UniTask LoadSingleAsync()
        {
            return UniTask.RunOnThreadPool(() =>
            {
                LoadSingle();
            });
        }
        private void SaveMultiple()
        {
            try
            {
                U archiveTable = new U();
                m_CurrentArchiveData1 = new T1();
                m_CurrentArchiveData2 = new T2();
                m_CurrentArchiveData3 = new T3();
                foreach (var data in m_ArchiveList)
                {
                    data.GetData(m_CurrentArchiveData1, m_CurrentArchiveData2, m_CurrentArchiveData3);
                }
                m_CurrentArchiveData1.TimeStamp = archiveTable.TimeStamp;
                m_CurrentArchiveData2.TimeStamp = archiveTable.TimeStamp;
                m_CurrentArchiveData3.TimeStamp = archiveTable.TimeStamp;
                int existingIndex = m_ArchiveTableList.FindIndex(t => t.TimeStamp == archiveTable.TimeStamp);
                if (existingIndex > 0)
                {
                    m_ArchiveTableList[existingIndex] = archiveTable;
                    m_ArchiveDataList1[existingIndex] = m_CurrentArchiveData1;
                    m_ArchiveDataList2[existingIndex] = m_CurrentArchiveData2;
                    m_ArchiveDataList3[existingIndex] = m_CurrentArchiveData3;
                }
                else
                {
                    m_ArchiveTableList.Add(archiveTable);
                    m_ArchiveDataList1.Add(m_CurrentArchiveData1);
                    m_ArchiveDataList2.Add(m_CurrentArchiveData2);
                    m_ArchiveDataList3.Add(m_CurrentArchiveData3);
                }
                MarkCurrentLoaded(m_CurrentArchiveData1.TimeStamp);
                string json1 = JsonConvert.SerializeObject(m_ArchiveTableList);
                using (StreamWriter sw = new StreamWriter(Path.Combine(m_MainFolderPath, ArchiveSetting.ARCHIVETABLE), false))
                {
                    sw.Write(json1);
                }
                string json2 = JsonConvert.SerializeObject(m_CurrentArchiveData1);
                string json3 = JsonConvert.SerializeObject(m_CurrentArchiveData2);
                string json4 = JsonConvert.SerializeObject(m_CurrentArchiveData3);
                if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                {
                    json2 = Rijindael.Encrypt(json2, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                    json3 = Rijindael.Encrypt(json3, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                    json4 = Rijindael.Encrypt(json4, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                }
                string childFolder1 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData1.GetType());
                string childFolder2 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData2.GetType());
                string childFolder3 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData3.GetType());
                if (string.IsNullOrEmpty(childFolder1) || string.IsNullOrEmpty(childFolder2) || string.IsNullOrEmpty(childFolder3))
                {
                    Debug.LogError("获取子文件夹失败，存档保存失败");
                    return;
                }
                string filePath1 = Path.Combine(childFolder1, $"{archiveTable.TimeStamp}.json");
                string filePath2 = Path.Combine(childFolder2, $"{archiveTable.TimeStamp}.json");
                string filePath3 = Path.Combine(childFolder3, $"{archiveTable.TimeStamp}.json");
                if (File.Exists(filePath1))
                {
                    File.Delete(filePath1);
                }
                if (File.Exists(filePath2))
                {
                    File.Delete(filePath2);
                }
                if (File.Exists(filePath3))
                {
                    File.Delete(filePath3);
                }
                using (StreamWriter sw = new StreamWriter(filePath1, false))
                {
                    sw.Write(json2);
                }
                using (StreamWriter sw = new StreamWriter(filePath2, false))
                {
                    sw.Write(json3);
                }
                using (StreamWriter sw = new StreamWriter(filePath3, false))
                {
                    sw.Write(json4);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"存档失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 覆写存档，传入时间戳参数以指定存档文件名，适用于多存档模式
        /// </summary>
        /// <param name="timeStamp"></param>
        public void Save(long timeStamp)
        {
            try
            {
                U archiveTable = new U()
                {
                    TimeStamp = timeStamp,
                };
                m_CurrentArchiveData1 = new T1();
                m_CurrentArchiveData2 = new T2();
                m_CurrentArchiveData3 = new T3();
                foreach (var data in m_ArchiveList)
                {
                    data.GetData(m_CurrentArchiveData1, m_CurrentArchiveData2, m_CurrentArchiveData3);
                }
                m_CurrentArchiveData1.TimeStamp = archiveTable.TimeStamp;
                m_CurrentArchiveData2.TimeStamp = archiveTable.TimeStamp;
                m_CurrentArchiveData3.TimeStamp = archiveTable.TimeStamp;
                int existingIndex = m_ArchiveTableList.FindIndex(t => t.TimeStamp == archiveTable.TimeStamp);
                if (existingIndex > 0)
                {
                    m_ArchiveTableList[existingIndex] = archiveTable;
                    m_ArchiveDataList1[existingIndex] = m_CurrentArchiveData1;
                    m_ArchiveDataList2[existingIndex] = m_CurrentArchiveData2;
                    m_ArchiveDataList3[existingIndex] = m_CurrentArchiveData3;
                }
                else
                {
                    m_ArchiveTableList.Add(archiveTable);
                    m_ArchiveDataList1.Add(m_CurrentArchiveData1);
                    m_ArchiveDataList2.Add(m_CurrentArchiveData2);
                    m_ArchiveDataList3.Add(m_CurrentArchiveData3);
                }
                MarkCurrentLoaded(m_CurrentArchiveData1.TimeStamp);
                string json1 = JsonConvert.SerializeObject(m_ArchiveTableList);
                using (StreamWriter sw = new StreamWriter(Path.Combine(m_MainFolderPath, ArchiveSetting.ARCHIVETABLE), false))
                {
                    sw.Write(json1);
                }
                string json2 = JsonConvert.SerializeObject(m_CurrentArchiveData1);
                string json3 = JsonConvert.SerializeObject(m_CurrentArchiveData2);
                string json4 = JsonConvert.SerializeObject(m_CurrentArchiveData3);
                if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                {
                    json2 = Rijindael.Encrypt(json2, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                    json3 = Rijindael.Encrypt(json3, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                    json4 = Rijindael.Encrypt(json4, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                }
                string childFolder1 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData1.GetType());
                string childFolder2 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData2.GetType());
                string childFolder3 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData3.GetType());
                if (string.IsNullOrEmpty(childFolder1) || string.IsNullOrEmpty(childFolder2) || string.IsNullOrEmpty(childFolder3))
                {
                    Debug.LogError("获取子文件夹失败，存档保存失败");
                    return;
                }
                string filePath1 = Path.Combine(childFolder1, $"{archiveTable.TimeStamp}.json");
                string filePath2 = Path.Combine(childFolder2, $"{archiveTable.TimeStamp}.json");
                string filePath3 = Path.Combine(childFolder3, $"{archiveTable.TimeStamp}.json");
                if (File.Exists(filePath1))
                {
                    File.Delete(filePath1);
                }
                if (File.Exists(filePath2))
                {
                    File.Delete(filePath2);
                }
                if (File.Exists(filePath3))
                {
                    File.Delete(filePath3);
                }
                using (StreamWriter sw = new StreamWriter(filePath1, false))
                {
                    sw.Write(json2);
                }
                using (StreamWriter sw = new StreamWriter(filePath2, false))
                {
                    sw.Write(json3);
                }
                using (StreamWriter sw = new StreamWriter(filePath3, false))
                {
                    sw.Write(json4);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"存档失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 异步保存存档，传入时间戳参数以指定存档文件名，适用于多存档模式
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <returns></returns>
        public async UniTask SaveAsync(long timeStamp)
        {
            await UniTask.RunOnThreadPool(async () =>
            {
                try
                {
                    U archiveTable = new U()
                    {
                        TimeStamp = timeStamp,
                    };
                    m_CurrentArchiveData1 = new T1();
                    m_CurrentArchiveData2 = new T2();
                    m_CurrentArchiveData3 = new T3();
                    foreach (var data in m_ArchiveList)
                    {
                        data.GetData(m_CurrentArchiveData1, m_CurrentArchiveData2, m_CurrentArchiveData3);
                    }
                    m_CurrentArchiveData1.TimeStamp = archiveTable.TimeStamp;
                    m_CurrentArchiveData2.TimeStamp = archiveTable.TimeStamp;
                    m_CurrentArchiveData3.TimeStamp = archiveTable.TimeStamp;
                    int existingIndex = m_ArchiveTableList.FindIndex(t => t.TimeStamp == archiveTable.TimeStamp);
                    if (existingIndex >= 0)
                    {
                        m_ArchiveTableList[existingIndex] = archiveTable;
                        m_ArchiveDataList1[existingIndex] = m_CurrentArchiveData1;
                        m_ArchiveDataList2[existingIndex] = m_CurrentArchiveData2;
                        m_ArchiveDataList3[existingIndex] = m_CurrentArchiveData3;
                    }
                    else
                    {
                        m_ArchiveTableList.Add(archiveTable);
                        m_ArchiveDataList1.Add(m_CurrentArchiveData1);
                        m_ArchiveDataList2.Add(m_CurrentArchiveData2);
                        m_ArchiveDataList3.Add(m_CurrentArchiveData3);
                    }
                    MarkCurrentLoaded(m_CurrentArchiveData1.TimeStamp);
                    string json1 = JsonConvert.SerializeObject(m_ArchiveTableList);
                    using (StreamWriter sw = new StreamWriter(Path.Combine(m_MainFolderPath, ArchiveSetting.ARCHIVETABLE), false))
                    {
                        await sw.WriteAsync(json1);
                    }
                    string json2 = JsonConvert.SerializeObject(m_CurrentArchiveData1);
                    string json3 = JsonConvert.SerializeObject(m_CurrentArchiveData2);
                    string json4 = JsonConvert.SerializeObject(m_CurrentArchiveData3);
                    if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                    {
                        json2 = Rijindael.Encrypt(json2, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                        json3 = Rijindael.Encrypt(json3, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                        json4 = Rijindael.Encrypt(json4, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                    }
                    string filePath1 = Path.Combine(ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData1.GetType()), $"{archiveTable.TimeStamp}.json");
                    string filePath2 = Path.Combine(ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData2.GetType()), $"{archiveTable.TimeStamp}.json");
                    string filePath3 = Path.Combine(ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData3.GetType()), $"{archiveTable.TimeStamp}.json");
                    if (File.Exists(filePath1))
                    {
                        File.Delete(filePath1);
                    }
                    if (File.Exists(filePath2))
                    {
                        File.Delete(filePath2);
                    }
                    if (File.Exists(filePath3))
                    {
                        File.Delete(filePath3);
                    }
                    using (StreamWriter sw = new StreamWriter(filePath1, false))
                    {
                        await sw.WriteAsync(json2);
                    }
                    using (StreamWriter sw = new StreamWriter(filePath2, false))
                    {
                        await sw.WriteAsync(json3);
                    }
                    using (StreamWriter sw = new StreamWriter(filePath3, false))
                    {
                        await sw.WriteAsync(json4);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"存档失败: {ex.Message}");
                }
            });
        }
        private async UniTask SaveMultipleAsync()
        {
            await UniTask.RunOnThreadPool(async () =>
            {
                try
                {
                    U archiveTable = new U();
                    m_CurrentArchiveData1 = new T1();
                    m_CurrentArchiveData2 = new T2();
                    m_CurrentArchiveData3 = new T3();
                    foreach (var data in m_ArchiveList)
                    {
                        data.GetData(m_CurrentArchiveData1, m_CurrentArchiveData2, m_CurrentArchiveData3);
                    }
                    m_CurrentArchiveData1.TimeStamp = archiveTable.TimeStamp;
                    m_CurrentArchiveData2.TimeStamp = archiveTable.TimeStamp;
                    m_CurrentArchiveData3.TimeStamp = archiveTable.TimeStamp;
                    int existingIndex = m_ArchiveTableList.FindIndex(t => t.TimeStamp == archiveTable.TimeStamp);
                    if (existingIndex >= 0)
                    {
                        m_ArchiveTableList[existingIndex] = archiveTable;
                        m_ArchiveDataList1[existingIndex] = m_CurrentArchiveData1;
                        m_ArchiveDataList2[existingIndex] = m_CurrentArchiveData2;
                        m_ArchiveDataList3[existingIndex] = m_CurrentArchiveData3;
                    }
                    else
                    {
                        m_ArchiveTableList.Add(archiveTable);
                        m_ArchiveDataList1.Add(m_CurrentArchiveData1);
                        m_ArchiveDataList2.Add(m_CurrentArchiveData2);
                        m_ArchiveDataList3.Add(m_CurrentArchiveData3);
                    }
                    MarkCurrentLoaded(m_CurrentArchiveData1.TimeStamp);
                    string json1 = JsonConvert.SerializeObject(m_ArchiveTableList);
                    using (StreamWriter sw = new StreamWriter(Path.Combine(m_MainFolderPath, ArchiveSetting.ARCHIVETABLE), false))
                    {
                        await sw.WriteAsync(json1);
                    }
                    string json2 = JsonConvert.SerializeObject(m_CurrentArchiveData1);
                    string json3 = JsonConvert.SerializeObject(m_CurrentArchiveData2);
                    string json4 = JsonConvert.SerializeObject(m_CurrentArchiveData3);
                    if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                    {
                        json2 = Rijindael.Encrypt(json2, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                        json3 = Rijindael.Encrypt(json3, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                        json4 = Rijindael.Encrypt(json4, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                    }
                    string filePath1 = Path.Combine(ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData1.GetType()), $"{archiveTable.TimeStamp}.json");
                    string filePath2 = Path.Combine(ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData2.GetType()), $"{archiveTable.TimeStamp}.json");
                    string filePath3 = Path.Combine(ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData3.GetType()), $"{archiveTable.TimeStamp}.json");
                    if (File.Exists(filePath1))
                    {
                        File.Delete(filePath1);
                    }
                    if (File.Exists(filePath2))
                    {
                        File.Delete(filePath2);
                    }
                    if (File.Exists(filePath3))
                    {
                        File.Delete(filePath3);
                    }
                    using (StreamWriter sw = new StreamWriter(filePath1, false))
                    {
                        await sw.WriteAsync(json2);
                    }
                    using (StreamWriter sw = new StreamWriter(filePath2, false))
                    {
                        await sw.WriteAsync(json3);
                    }
                    using (StreamWriter sw = new StreamWriter(filePath3, false))
                    {
                        await sw.WriteAsync(json4);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"存档失败: {ex.Message}");
                }
            });
        }
        private void LoadMultiple()
        {
            try
            {
                string archiveTablePath = Path.Combine(m_MainFolderPath, ArchiveSetting.ARCHIVETABLE);
                if (File.Exists(archiveTablePath))
                {
                    using (StreamReader sr = new StreamReader(archiveTablePath))
                    {
                        string json = sr.ReadToEnd();
                        m_ArchiveTableList = JsonConvert.DeserializeObject<List<U>>(json);
                    }
                    m_ArchiveDataList1.Clear();
                    m_ArchiveDataList2.Clear();
                    m_ArchiveDataList3.Clear();
                    string childFolder1 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData1.GetType());
                    string childFolder2 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData2.GetType());
                    string childFolder3 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData3.GetType());
                    if (string.IsNullOrEmpty(childFolder1) || string.IsNullOrEmpty(childFolder2) || string.IsNullOrEmpty(childFolder3))
                    {
                        Debug.LogError("获取子文件夹失败，存档加载失败");
                        return;
                    }
                    foreach (var file in m_ArchiveTableList)
                    {
                        string archiveDataPath1 = Path.Combine(childFolder1, $"{file.TimeStamp}.json");
                        string archiveDataPath2 = Path.Combine(childFolder2, $"{file.TimeStamp}.json");
                        string archiveDataPath3 = Path.Combine(childFolder3, $"{file.TimeStamp}.json");
                        if (File.Exists(archiveDataPath1))
                        {
                            using (StreamReader sr2 = new StreamReader(archiveDataPath1))
                            {
                                string json2 = sr2.ReadToEnd();
                                if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                                {
                                    json2 = Rijindael.Decrypt(json2, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                                }
                                m_ArchiveDataList1.Add(JsonConvert.DeserializeObject<T1>(json2));
                            }
                        }
                        if (File.Exists(archiveDataPath2))
                        {
                            using (StreamReader sr2 = new StreamReader(archiveDataPath2))
                            {
                                string json2 = sr2.ReadToEnd();
                                if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                                {
                                    json2 = Rijindael.Decrypt(json2, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                                }
                                m_ArchiveDataList2.Add(JsonConvert.DeserializeObject<T2>(json2));
                            }
                        }
                        if (File.Exists(archiveDataPath3))
                        {
                            using (StreamReader sr2 = new StreamReader(archiveDataPath3))
                            {
                                string json2 = sr2.ReadToEnd();
                                if (m_ArchiveSetting.EncryptionType == EncryptionType.AES)
                                {
                                    json2 = Rijindael.Decrypt(json2, m_ArchiveSetting.KEY, m_ArchiveSetting.IV);
                                }
                                m_ArchiveDataList3.Add(JsonConvert.DeserializeObject<T3>(json2));
                            }
                        }
                    }
                    if (m_ArchiveDataList1.Count > 0 && m_ArchiveDataList2.Count > 0 && m_ArchiveDataList3.Count > 0)
                    {
                        if (!HasMarkedLoaded())
                        {
                            m_CurrentArchiveData1 = m_ArchiveDataList1[m_ArchiveDataList1.Count - 1];
                            m_CurrentArchiveData2 = m_ArchiveDataList2[m_ArchiveDataList2.Count - 1];
                            m_CurrentArchiveData3 = m_ArchiveDataList3[m_ArchiveDataList3.Count - 1];
                            MarkCurrentLoaded(m_CurrentArchiveData1.TimeStamp);
                        }
                        else
                        {
                            foreach (var data in m_ArchiveTableList)
                            {
                                if (data.IsLoaded)
                                {
                                    m_CurrentArchiveData1 = m_ArchiveDataList1.Find(x => x.TimeStamp == data.TimeStamp);
                                    m_CurrentArchiveData2 = m_ArchiveDataList2.Find(x => x.TimeStamp == data.TimeStamp);
                                    m_CurrentArchiveData3 = m_ArchiveDataList3.Find(x => x.TimeStamp == data.TimeStamp);
                                    break;
                                }
                            }
                        }
                        foreach (var data in m_ArchiveList)
                        {
                            data.SetData(m_CurrentArchiveData1, m_CurrentArchiveData2, m_CurrentArchiveData3);
                        }
                    }
                    else
                    {
                        Debug.LogError("存档数据列表为空");
                    }
                }
                else
                {
                    Debug.LogError($"存档表文件不存在: {archiveTablePath}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"读取存档失败: {ex.Message}");
            }
        }
        private UniTask LoadMultipleAsync()
        {
            return UniTask.RunOnThreadPool(() =>
            {
                LoadMultiple();
            });
        }
        /// <summary>
        /// 删除当前存档，适用于单存档模式
        /// </summary>
        public void Delete()
        {
            try
            {
                string childFolder1 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData1.GetType());
                string childFolder2 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData2.GetType());
                string childFolder3 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData3.GetType());
                if (string.IsNullOrEmpty(childFolder1) || string.IsNullOrEmpty(childFolder2) || string.IsNullOrEmpty(childFolder3))
                {
                    Debug.LogError("获取子文件夹失败，存档删除失败");
                    return;
                }
                string filePath1 = Path.Combine(childFolder1, $"{m_ArchiveSetting.FileName}");
                string filePath2 = Path.Combine(childFolder2, $"{m_ArchiveSetting.FileName}");
                string filePath3 = Path.Combine(childFolder3, $"{m_ArchiveSetting.FileName}");
                if (File.Exists(filePath1))
                {
                    File.Delete(filePath1);
                }
                if (File.Exists(filePath2))
                {
                    File.Delete(filePath2);
                }
                if (File.Exists(filePath3))
                {
                    File.Delete(filePath3);
                }
                Debug.Log("存档删除成功");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"删除存档失败: {ex.Message}");
            }
        }
        /// <summary>
        /// 删除指定时间戳的存档，适用于多存档模式
        /// </summary>
        /// <param name="timestamp"></param>
        public void Delete(long timestamp)
        {
            try
            {
                string childFolder1 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData1.GetType());
                string childFolder2 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData2.GetType());
                string childFolder3 = ArchiveSystemHelper.GetChildFolder(m_MainFolderPath, m_CurrentArchiveData3.GetType());
                if (string.IsNullOrEmpty(childFolder1) || string.IsNullOrEmpty(childFolder2) || string.IsNullOrEmpty(childFolder3))
                {
                    Debug.LogError("获取子文件夹失败，存档删除失败");
                    return;
                }
                string filePath1 = Path.Combine(childFolder1, $"{timestamp}.json");
                string filePath2 = Path.Combine(childFolder2, $"{timestamp}.json");
                string filePath3 = Path.Combine(childFolder3, $"{timestamp}.json");
                if (File.Exists(filePath1))
                {
                    File.Delete(filePath1);
                }
                if (File.Exists(filePath2))
                {
                    File.Delete(filePath2);
                }
                if (File.Exists(filePath3))
                {
                    File.Delete(filePath3);
                }
                m_ArchiveTableList.RemoveAll(x => x.TimeStamp == timestamp);
                m_ArchiveDataList1.RemoveAll(x => x.TimeStamp == timestamp);
                m_ArchiveDataList2.RemoveAll(x => x.TimeStamp == timestamp);
                m_ArchiveDataList3.RemoveAll(x => x.TimeStamp == timestamp);
                // 更新存档表文件
                string json = JsonConvert.SerializeObject(m_ArchiveTableList);
                using (StreamWriter sw = new StreamWriter(Path.Combine(m_MainFolderPath, ArchiveSetting.ARCHIVETABLE), false))
                {
                    sw.Write(json);
                }
                Debug.Log("存档删除成功");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"删除存档失败: {ex.Message}");
            }
        }
        private void MarkCurrentLoaded(long timestamp)
        {
            foreach (var data in m_ArchiveTableList)
            {
                if (data.TimeStamp == timestamp)
                {
                    data.IsLoaded = true;
                }
                else
                {
                    data.IsLoaded = false;
                }
            }
        }
        private bool HasMarkedLoaded()
        {
            foreach (var data in m_ArchiveTableList)
            {
                if (data.IsLoaded)
                {
                    return true;
                }
            }
            return false;
        }
        public interface IArchive
        {
            void Register(IArchive data) => Instance.Register(data);
            void Unregister(IArchive data) => Instance.Unregister(data);
            void GetData(T1 data1, T2 data2, T3 data3);
            void SetData(T1 data1, T2 data2, T3 data3);
        }
    }
}


