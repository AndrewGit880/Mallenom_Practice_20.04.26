from rest_framework.decorators import api_view
from rest_framework.response import Response
from rest_framework import status
from .models import User
from .serializers import UserSerializer, LoginSerializer

@api_view(['POST'])
def login(request):
    serializer = LoginSerializer(data=request.data)
    if not serializer.is_valid():
        return Response(serializer.errors, status=status.HTTP_400_BAD_REQUEST)
    
    login = serializer.validated_data['login']
    password = serializer.validated_data['password']
    
    try:
        user = User.objects.get(login=login)
        if user.check_password(password):
            return Response({
                'user': UserSerializer(user).data,
                'message': 'Вход выполнен успешно'
            })
        else:
            return Response({'error': 'Неверный пароль'}, status=status.HTTP_401_UNAUTHORIZED)
    except User.DoesNotExist:
        return Response({'error': 'Пользователь не найден'}, status=status.HTTP_401_UNAUTHORIZED)

@api_view(['POST'])
def logout(request):
    return Response({'message': 'Выход выполнен'})

@api_view(['GET'])
def get_current_user(request):
    user_id = request.headers.get('X-User-Id')
    if user_id:
        try:
            user = User.objects.get(id=user_id)
            return Response(UserSerializer(user).data)
        except User.DoesNotExist:
            pass
    return Response({'error': 'Не авторизован'}, status=status.HTTP_401_UNAUTHORIZED)