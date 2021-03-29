using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UHelper;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class LicenseUI : UIBase {
    const string machineNumber_key = "machine_key";
    const string encrypter_key = "license_key";
    public bool IsValid () {
        try {
            var _local_license_key = PlayerPrefs.GetString (encrypter_key, "unset");
            if (_local_license_key == "unset") return false;
            var _decrypt = Encrypter.Decrypt (_local_license_key, "mrbaoquan");
            if (_decrypt == machineID) {
                return true;
            }
        } catch (System.Exception) {
            return false;
        }
        return false;
    }

    public void Check () {
        if (!IsValid ()) {
            Managements.UI.Show<LicenseUI> ();
        }
    }

    private string machineID {
        get {
            string _machineID = PlayerPrefs.GetString (machineNumber_key, "unset");
            if (_machineID == "unset") {
                _machineID = UnityEngine.Random.Range (100000, 999999).ToString ();
                PlayerPrefs.SetString (machineNumber_key, _machineID);
            }
            return _machineID;
        }
    }

    private ReactiveProperty<string> licenseContent = new ReactiveProperty<string> (string.Empty);
    // Start is called before the first frame update
    private void Start () {
        this.Get<InputField> ("input_license").OnValueChangedAsObservable ().Subscribe (_ => {
            licenseContent.Value = _;
        });

        licenseContent.Subscribe (_ => {
            this.Get<Button> ("btn_active").gameObject.SetActive (_.Length >= 128);
        });

        this.Get<Text> ("text_machineNumber").text = machineID;
        this.Get<Button> ("btn_active").OnClickAsObservable ().Subscribe (_1 => {
            try {
                string _input_key = Encrypter.Decrypt (licenseContent.Value, "mrbaoquan");
                if (_input_key == machineID) {
                    Managements.UI.ShowAlert ("序列号有效, 软件已成功激活!", () => {
                        PlayerPrefs.SetString (encrypter_key, licenseContent.Value);
                        PlayerPrefs.SetString (machineNumber_key, machineID);
                        Managements.UI.Hide ("LicenseUI");
                    });
                } else {
                    Managements.UI.ShowAlert ("请输入正确的激活码!");
                }
            } catch (System.Exception) {
                Managements.UI.ShowAlert ("请输入正确的激活码!");
                throw;
            }

        });
    }

    // Update is called once per frame
    private void Update () {

    }

    // Called when this ui is showing
    protected override void OnShow () {

    }

    // Called when this ui is hidden
    protected override void OnHidden () {

    }
}