using UnityEngine;
using UnityEngine.UI;
using UNIHper;
using TMPro;
using DNHper;
using System.Text.RegularExpressions;
using System;

namespace UNIHper.UI
{
    using UniRx;

    public class LicenseUI : UIBase
    {
        const string machineNumber_key = "machine_id";
        const string license_key = "app_license";
        const string register_at_key = "register_at";

        const string passphrase = "mrbaoquan";

        public string LicenseText
        {
            get
            {
                var _licenseContent = PlayerPrefs.GetString(license_key, "unset");
                if (_licenseContent == "unset")
                    return "无授权信息";
                var (_machineID, _expireDate) = parseLicense(_licenseContent);

                var _remainDays = (_expireDate - DateTime.Now.Date).Days;

                var _dayTip = _remainDays < 0 ? "  已过期: " : "  剩余: ";

                return "许可证有效期至: "
                    + _expireDate.ToString("yyyy-MM-dd")
                    + _dayTip
                    + Mathf.Abs(_remainDays).ToString()
                    + " 天";
            }
        }

        public bool IsValid()
        {
            var _licenseContent = PlayerPrefs.GetString(license_key, "unset");
            if (_licenseContent == "unset")
                return false;

            var _registerAt = PlayerPrefs.GetString(register_at_key, "unset");
            if (_registerAt == "unset")
                return false;

            _registerAt = AES.Decrypt(_registerAt, passphrase);
            var _registerDate = DateTime.Parse(_registerAt);

            // 如果当前日期小于注册日期, 则说明用户修改了系统时间
            if (DateTime.Now.Date < _registerDate)
                return false;

            return isLicenseValid(_licenseContent);
        }

        public void Check()
        {
            if (!IsValid())
            {
                Managements.UI.Show<LicenseUI>();
                return;
            }
            licenseValid.Value = true;
        }

        private string machineID
        {
            get
            {
                string _machineID = PlayerPrefs.GetString(machineNumber_key, "unset");
                if (_machineID == "unset")
                {
                    _machineID = UnityEngine.Random.Range(100000, 999999).ToString();
                    _machineID = AES.Encrypt(_machineID, passphrase);
                    PlayerPrefs.SetString(machineNumber_key, _machineID);
                }
                try
                {
                    // 当machineID被修改后, 会导致解密失败, 这里捕获异常, 重新生成machineID
                    _machineID = AES.Decrypt(_machineID, passphrase);
                }
                catch (System.Exception)
                {
                    _machineID = UnityEngine.Random.Range(100000, 999999).ToString();
                    _machineID = AES.Encrypt(_machineID, passphrase);
                    PlayerPrefs.SetString(machineNumber_key, _machineID);
                    _machineID = AES.Decrypt(_machineID, passphrase);
                }

                return _machineID;
            }
        }

        private ReactiveProperty<string> licenseContent = new ReactiveProperty<string>(
            string.Empty
        );
        private ReactiveProperty<bool> licenseValid = new ReactiveProperty<bool>(false);
        public IObservable<bool> OnLicenseValidChanged => licenseValid.AsObservable();

        // Start is called before the first frame update
        private void Start() { }

        private (string machineID, DateTime expireDate) parseLicense(string license)
        {
            // 明文
            var _plainText = AES.Decrypt(license, "mrbaoquan");
            var _regex = new Regex(@"^(\d{6})@(\d{4}-\d{2}-\d{2})$");
            var _match = _regex.Match(_plainText);
            if (!_match.Success)
            {
                return (string.Empty, DateTime.MinValue);
            }
            var _machineID = _match.Groups[1].Value;
            var _expireDate = _match.Groups[2].Value;
            return (_machineID, System.DateTime.Parse(_expireDate));
        }

        private bool isLicenseValid(string license)
        {
            try
            {
                var (_machineID, _expireDate) = parseLicense(license);
                if (_machineID != machineID || _expireDate < DateTime.Now)
                {
                    return false;
                }
            }
            catch (System.Exception)
            {
                return false;
            }

            return true;
        }

        // Update is called once per frame
        private void Update() { }

        protected override void OnLoaded()
        {
            this.Get<TMP_InputField>("input_field/input_license")
                .onValueChanged.AsObservable()
                .Subscribe(_ =>
                {
                    licenseContent.Value = _;
                });

            Button _btnActive = this.Get<Button>("btn_active");
            licenseContent.Subscribe(_ =>
            {
                _btnActive.interactable = _.Length >= 64;
            });

            this.Get<TMP_InputField>("input_field/input_license").text = PlayerPrefs.GetString(
                license_key,
                string.Empty
            );

            this.Get<Text>("text_MACNumber").text = machineID;
            _btnActive
                .OnClickAsObservable()
                .Subscribe(_1 =>
                {
                    if (!isLicenseValid(licenseContent.Value))
                    {
                        Managements.UI.ShowAlert("许可证无效或已过期，请重新输入!");
                        return;
                    }

                    licenseValid.SetValueAndForceNotify(true);

                    Managements.UI.ShowAlert(
                        "授权成功 \n" + LicenseText,
                        () =>
                        {
                            PlayerPrefs.SetString(license_key, licenseContent.Value);
                            var _registerAt = DateTime.Now.ToString("yyyy-MM-dd");
                            PlayerPrefs.SetString(
                                register_at_key,
                                AES.Encrypt(_registerAt, passphrase)
                            );
                            Managements.UI.Hide<LicenseUI>();
                            licenseValid.SetValueAndForceNotify(true);
                        }
                    );
                });

            this.Get<Button>("btn_close")
                .OnClickAsObservable()
                .Subscribe(_ =>
                {
                    this.Hide();
                });

            licenseValid.Subscribe(_valid =>
            {
                this.Get("btn_close").SetActive(_valid);
                this.Get<TextMeshProUGUI>("text_license").text = LicenseText;
                this.Get<TextMeshProUGUI>("title").text = _valid ? "软件授权管理" : "软件需要授权";
                this.Get<TextMeshProUGUI>("btn_active/Text (TMP)")
                    .SetText(_valid ? "重新授权" : "请求授权");
            });
        }

        // Called when this ui is showing
        protected override void OnShown() { }

        // Called when this ui is hidden
        protected override void OnHidden() { }
    }
}
