   M S B A M L   `   `   `  ����   WifiWPFClient    WifiWPFClient.MainWindow   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     -     �Name$  
mainWindow�� � Title$ 
MainWindow��$	��603��$	��914��.��� $��0,2,0,-2q�.+��� -    ��Name$ m_Log��$
��Left=�$	��553�� �� TextWrapping$
 Wrap=�$	��Top=�$	��537��$��
359,10,0,0q� �� AcceptsReturn .  J� RenderTransformOrigin$ 0.5,0.5&��� -    ��Name$ 
bt_Refresh��$��Refresh��$
��Left=�$	��Top=�$	��120��$��
10,169,0,0q��� -    ��Name$ lb_networks��$��Stretch=� �� SelectionMode$ Extended=�$
��Left=�$	��266��$��
10,196,0,0q�$	��Top=�$	��328������ .R�� $	��0,2q�	 � ColumnDefinitions	 �� $��* ��� $	��250 �.+��� 
 ��Name$
 tb_SSIDName��$
��Left=����� *
SSIDName+�  �Name$ pb_SignalQuality��$��1��$	��100�� � Minimum$ 0Q� � Maximum$	 100Q�$��Stretch=� � Value �� *SignalQuality+�� $��1��$��Center=�$��Center=�����  �� ElementName$ pb_SignalQuality�� �� Path$ Value� �� StringFormat$ {0:0}%���� -    ��Name$ tc_RegisterLogin��$
��Left=�$	��154��$��	10,10,0,0q�$	��Top=�$	��331��.@��� -    ��Name$ ti_Login��$��Giriş��.��� ���	#FFE5E5E5$��0q�.+��� -   $ tb_Email��$
��Left=�$��22��$��	96,10,0,0q�$
 Wrap=�$	��Top=�$	��120��G� -    G�Name$ tb_Password��$
��Left=�$��22��$��	96,37,0,0q�$	��Top=�$	��120���� $��
E-Posta : ��$
��Left=�$��0,8,0,0q�$	��Top=��� $��	Parola : ��$
��Left=�$��0,35,0,0q�$	��Top=��� -	   $ 
bt_Connect��$
��Left=�$	��Top=�$	��120��$��	96,64,0,0q�$��22������ *bt_ConnectContent+�� -
    ��Name$ 
l_defQuota��$��Kota : ��$
��Left=�$	��Top=�$��0,91,0,0q��� -   $ l_Quota��$
��Left=�$	��Top=�$��	96,91,0,0q�$	��120��$��26������ *
QuotaStr+�� -    ��Name$ cb_CEmailRemember��$��Hatırla��$
��Left=�$��
223,13,0,0q�$	��Top=�$��88���� -   $ cb_CPassRemember��$��Hatırla��$
��Left=�$��
223,40,0,0q�$	��Top=�$��88���� -   $ ti_Register��$��Kayıt��.��� ���	#FFE5E5E5.+��� -   $ tb_RegisterEmail��$
��Left=�$��22��$��	96,10,0,0q�$
 Wrap=�$	��Top=�$	��120��$��ercument@outlook.com��G� -   $ tb_RegisterPassword1��$
��Left=�$��22��$��	96,37,0,0q�$	��Top=�$	��120�� G� Password$ e���� $��
E-Posta : ��$
��Left=�$��0,8,0,0q�$	��Top=��� $��	Parola : ��$
��Left=�$��0,35,0,0q�$	��Top=�G� -   $ tb_RegisterPassword2��$
��Left=�$��22��$��	96,64,0,0q�$	��Top=�$	��120��$ e���� $��Parola (Tekrar) : ��$
��Left=�$��0,62,0,0q�$	��Top=��� -   $ bt_Register��$��Register��$
��Left=�$	��Top=�$	��120��$��	96,91,0,0q�