package com.HoegidongMakguli.Quoridor;

import android.os.Build;
import android.os.Bundle;
import com.unity3d.player.UnityPlayerActivity;

public class DeviceInfo extends UnityPlayerActivity {

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
    }

    // Unity에서 호출할 함수: 기기 모델명 가져오기
    public static String GetDeviceModel() {
        return Build.MODEL;
    }

    // Unity에서 호출할 함수: 안드로이드 버전 가져오기
    public static String GetAndroidVersion() {
        return Build.VERSION.RELEASE;
    }
}