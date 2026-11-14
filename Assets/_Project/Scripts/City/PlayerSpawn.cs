using UnityEngine;

namespace BilgiAdasi.City
{
    /// <summary>
    /// Sahne bağlantı noktası: oyuncu burada doğar. Mavi ok (ileri) = bakış yönü.
    /// Sahnede tam bir tane olmalı (08-mustafa-entegrasyon.md).
    /// </summary>
    public sealed class PlayerSpawn : MonoBehaviour
    {
        [Tooltip("PlayerRig prefab'ı (oyuncu + Main Camera). Prefab'da hazır gelir; değiştirmeyin.")]
        [SerializeField] private GameObject playerRigPrefab;

        [Tooltip("Oyuncu doğma noktasının bu kadar altına düşerse (harita deliği, kenar) buraya geri ışınlanır.")]
        [SerializeField] private float fallLimit = 50f;

        public GameObject SpawnedRig { get; private set; }

        /// <summary>Kayıttaki oyuncu konumu anahtarı (SaveData.poses).</summary>
        public const string PlayerPoseKey = "player";

        private BilgiAdasi.Player.PlayerMotor motor;

        private void Awake()
        {
            if (playerRigPrefab == null)
            {
                Debug.LogError("[PlayerSpawn] PlayerRig prefab'ı atanmamış.", this);
                return;
            }

            // Devam: son otomatik kayıttaki konum ve bakış yönü; yeni oyunda doğma noktası.
            var position = transform.position;
            var rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            var data = Core.SaveSystem.Instance != null ? Core.SaveSystem.Instance.Data : null;
            if (data != null && data.TryGetPose(PlayerPoseKey, out var pose) && pose.position.y > transform.position.y - fallLimit)
            {
                position = pose.position;
                rotation = Quaternion.Euler(0f, pose.yaw, 0f);
            }
            SpawnedRig = Instantiate(playerRigPrefab, position, rotation);
            SpawnedRig.name = playerRigPrefab.name;
            motor = SpawnedRig.GetComponentInChildren<BilgiAdasi.Player.PlayerMotor>();
        }

        private void OnEnable() => Core.GameEvents.BeforeSave += OnBeforeSave;

        private void OnDisable() => Core.GameEvents.BeforeSave -= OnBeforeSave;

        // Otomatik kayıt anı: oyuncu yerdeyse konumu yazılır. Bisikletteyken (kapsül kapalı) Bicycle kendi konumunu yazar.
        private void OnBeforeSave(Core.SaveData data)
        {
            if (motor == null || !motor.Controller.enabled) return;
            var t = motor.transform;
            data.SetPose(PlayerPoseKey, t.position, t.eulerAngles.y);
        }

        private void Update()
        {
            // Bisikletteyken kapsül kapalıdır; o durumu Bicycle kendisi toparlar.
            if (motor == null || !motor.Controller.enabled) return;
            if (motor.transform.position.y < transform.position.y - fallLimit)
                motor.Teleport(transform.position, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, 0.35f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.6f);
            Vector3 eye = transform.position + Vector3.up * 1.56f;
            Gizmos.DrawLine(eye, eye + transform.forward * 1f);
            Gizmos.DrawWireSphere(eye + transform.forward * 1f, 0.06f);
        }
#endif
    }
}
