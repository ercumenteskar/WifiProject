   M S B A M L   `   `   `  ����G  CWifiProvider, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null[2clr-namespace:WifiSolution.WifiProvider.Properties$WifiSolution.WifiProvider.Properties  5   	   *    $WifiSolution.WifiProvider.MainWindow   5   	   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @p2clr-namespace:WifiSolution.WifiProvider.Properties     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     6F   -   5          �Name$  
mainWindow��5   �    � Title$ 
MainWindow��5   	   $	��350��6   $	��853��6)   .��5      � .+�5   
   �� -   5         ��Name$ label��5      $��Lütfen bekleyiniz...��6   $��Center=�6>   $��0,0,0,0q�6[   $��Center=�6l   � 5	   
   -   5        $��MainGrid��5	      y�6    �� *MainGridVisibility+    booleanToVisibilityConverter �� 	Converter# [  .+�5
      �� -   5         ��Name$ tb_Log��5
      $
��Left=�6;   $	��300��6V   $��
348,10,0,0q�6c    �� TextWrapping$
 Wrap=�6w   $	��Top=�6�   $	��487��6�   ��6&   �� *Log+6�   �� 5      -   5         ��Name$ tc_RegisterLogin��5      $
��Left=�6s   $	��196��6�   $��	10,10,0,0q�6�   $	��Top=�6�   $	��331��6�    �� SelectedIndex 63   �� *'%Account.tc_RegisterLoginSelectedIndex+.@�5      �� -   5         ��Name$ ti_Login��5      $��Giriş��6,   .��5      � ���	#FFE5E5E5$��0q�62   .+�5      �� -   5        $ tb_Email��5   "   $
��Left=�6�   $��23��6�   $��	96,10,0,0q�6�   $	��Top=�6�   $	��120��6  ��64   �� *Account.Email+	 �� Mode$	 TwoWay=�
 �� UpdateSourceTrigger$
 PropertyChanged=�{�6�   �� *Account.tb_EmailIsEnabled+6  G� 5      -   5         G�Name$ tb_Password��5   &   $
��Left=�6;   $��23��6�   $��	96,37,0,0q�6�   $	��Top=�6�   $	��120��6�   {�6V   �� *Account.tb_PasswordIsEnabled+6�   �� 5      $��
E-Posta : ��6    $
��Left=�65   $��0,8,0,0q�6P   $	��Top=�6a   �� 5      $��	Parola : ��6    $
��Left=�64   $��0,35,0,0q�6O   $	��Top=�6a   �� 5      -	   5         ��Name$ bt_Login��5   !   $
��Left=�6�   $	��Top=�6�   $	��120��6�   $��	96,64,0,0q�6�   $��22��6�   ��63   �� *Account.bt_LoginContent+{�6_   �� *Account.canLogin+��6�   �� *bt_LoginCommand+6�   �� 5      -
   5        $ 
l_defQuota��5       $��Kota : ��64   $
��Left=�6F   $	��Top=�6a   $��0,91,0,0q�6y   �� 5      -   5        $ l_Quota��5       $
��Left=�6V   $	��Top=�6q   $��	96,91,0,0q�6�   $	��120��6�   $��26��6�   ��61   �� *Account.QuotaStr+6�   �� 5      -   5         ��Name$ cb_PEmailRemember��5   #   $��Hatırla��6s   $
��Left=�6�   $��
223,13,0,0q�6�   $	��Top=�6�   $��88��6�    d� 	IsChecked 6>   �� * Account.EmailRememberIsChecked+6�   �� 5      -   5        $ cb_PPassRemember��5   #   $��Hatırla��6q   $
��Left=�6�   $��
223,40,0,0q�6�   $	��Top=�6�   $��88��6�    6=   �� *Account.PassRememberIsChecked+6�   �� 5      -   5        $ 	bt_Forgot��5   !   $
��Left=�6a   $	��Top=�6|   $��88��6�   $��
223,64,0,0q�6�   $��Forgot?��6�   $��22��6�   ��64   �� *Account.bt_ForgotCommand+6�   �� 5      -   5        $ 
bt_Connect��5   !   $
��Left=�6�   $	��Top=�6�   $��88��6�   $��223,132,0,0q�6�   $��22��6�   ��65   �� *bt_ConnectContent+{�6[   �� *
canConnect+��6|   �� *bt_ConnectCommand+6�   �� 5      -   5        $ cb_AutoLogin��5   #   $��Otomatik giriş��6a   $
��Left=�6z   $��
221,91,0,0q�6�   $	��Top=�6�   $��90��6�    69   �� *Account.AutoLogin+6�   �� 5      -   5        $ cb_AutoConnect��5   #   $��Otomatik bağlan��6e   $
��Left=�6   $��221,111,0,0q�6�   $	��Top=�6�   $��90��6�    6;   �� *Account.AutoConnect+6�   5      5      �� 5      -   5        $ ti_Register��5      $��Kayıt��6/   .��5      � ���	#FFE5E5E5.+�5      �� -   5        $ tb_RegisterEmail��5   "   $
��Left=�6c   $��23��6~   $��	96,10,0,0q�6�   $	��Top=�6�   $	��120��6�   ��6<   �� *Account.RegisterEmail+6�   G� 5       -   5        $ tb_RegisterPassword1��5    &   $
��Left=�6D   $��23��6_   $��	96,37,0,0q�6k   $	��Top=�6~   $	��120��6�   6�   �� 5!      $��
E-Posta : ��6    $
��Left=�65   $��0,8,0,0q�6P   $	��Top=�6a   �� 5"      $��	Parola : ��6    $
��Left=�64   $��0,35,0,0q�6O   $	��Top=�6a   G� 5#      -   5        $ tb_RegisterPassword2��5#   &   $
��Left=�6D   $��22��6_   $��	96,64,0,0q�6k   $	��Top=�6~   $	��120��6�   6�   �� 5$      $��Parola (Onay) : ��6    $
��Left=�6;   $��0,62,0,0q�6V   $	��Top=�6h   �� 5%      -   5        $ bt_Register��5%   !   $��Register��6e   $
��Left=�6x   $	��Top=�6�   $	��120��6�   $��	96,91,0,0q�6�   ��66   �� *Account.bt_RegisterCommand+6�   5&      5'      5(      5)      5*      5+      